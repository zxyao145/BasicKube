using BasicKube.Api.Common;
using BasicKube.Api.Controllers.App.Deploy;
using BasicKube.Api.Exceptions;
using Jil;
using k8s.Autorest;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

namespace BasicKube.Api.Controllers.App;

public class TerminalController : KubeControllerBase
{
    private readonly ILogger<TerminalController> _logger;
    private readonly KubernetesFactory _k8sFactory;

    public TerminalController(ILogger<TerminalController> logger, KubernetesFactory k8sFactory)
    {
        _logger = logger;
        _k8sFactory = k8sFactory;
    }

    /// <summary>
    /// 终端
    /// </summary>

    /// <param name="podName"></param>
    /// <param name="containerName"></param>
    /// <param name="ns"></param>
    /// <returns></returns>
    [HttpGet("{podName}/{containerName}")]
    public async Task Terminal(
        [FromRoute] string podName,
        [FromRoute] string containerName
        )
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await Proxy3(webSocket, new TerminalInfo()
            {
                NsName = NsName,
                ContainerName = containerName,
                PodName = podName
            });
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private static List<string> ValidBashs = new List<string>()
        {
            "/bin/bash","/bin/sh"
        };

    private async Task Proxy3(WebSocket clientWebSocket, TerminalInfo info)
    {
        var kubernetes = _k8sFactory.MustGet(info.PodName);
        var chIn = Channel.CreateUnbounded<byte[]>();
        var chOut = Channel.CreateUnbounded<byte[]>();

        WebSocket? webSocket = null;
        StreamDemuxer? demux = null;
        Stream? streamOutput = null;
        foreach (var bash in ValidBashs)
        {
            webSocket = await kubernetes.WebSocketNamespacedPodExecAsync(
                    info.PodName,
                    info.NsName,
                    new List<string>()
                    {
                            bash,
                    },
                    info.ContainerName
                )
                .ConfigureAwait(false);

            demux = new StreamDemuxer(webSocket);
            demux.Start();
            streamOutput = demux.GetStream(ChannelIndex.StdOut, null);

            var buff = new byte[1024];
            var read = await streamOutput.ReadAsync(buff, 0, buff.Length);
            if (read > 0)
            {
                Console.WriteLine($"{bash}: success");
                break;
            }
            else
            {
                Console.WriteLine($"{bash}: failed");
            }
        }

        Debug.Assert(webSocket != null);
        Debug.Assert(demux != null);
        Debug.Assert(streamOutput != null);

        var streamIn = demux.GetStream(null, ChannelIndex.StdIn);
        var resizeStream = demux.GetStream(null, ChannelIndex.Resize);
        var stdErr = demux.GetStream(ChannelIndex.StdErr, null);
        var error = demux.GetStream(ChannelIndex.Error, null);


        var pumpCancellation = new CancellationTokenSource();
        var accept = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    var clientBuf = new byte[4096];
                    var podBuf = new byte[4096];

                    var receiveResult = await clientWebSocket.ReceiveAsync(
                        new ArraySegment<byte>(clientBuf), CancellationToken.None);

                    while (!receiveResult.CloseStatus.HasValue)
                    {
                        await chIn.Writer.WriteAsync(
                            clientBuf.Take(receiveResult.Count).ToArray(),
                            pumpCancellation.Token
                            );
                        receiveResult = await clientWebSocket.ReceiveAsync(
                          new ArraySegment<byte>(clientBuf), pumpCancellation.Token);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("accept err:{0}", e);
                    break;
                }
            }
        });
        var forwardAccept = Task.Run(async () =>
        {
            while (await chIn.Reader.WaitToReadAsync(pumpCancellation.Token))
            {
                if (chIn.Reader.TryRead(out var arraySegment))
                {
                    var inputObj = Encoding.UTF8.GetString(arraySegment);
                    _logger.LogInformation("receive client:{0}", inputObj);

                    var jsonObj = JSON.DeserializeDynamic(inputObj);
                    if (jsonObj.type == "stdIn")
                    {
                        string input = jsonObj.input;
                        var bytes = Encoding.UTF8.GetBytes(input);
                        await streamIn.WriteAsync(bytes, pumpCancellation.Token);
                    }
                    else if (jsonObj.type == "resize")
                    {
                        int rows = jsonObj.rows;
                        int cols = jsonObj.cols;
                        var whInfo = new
                        {
                            Width = cols,
                            Height = rows,
                        };
                        var jsonStr = JSON.SerializeDynamic(whInfo);
                        // jsonStr = "{\"Height\":24,\"Width\":159}";
                        var bytes = Encoding.UTF8.GetBytes(jsonStr);
                        await resizeStream.WriteAsync(bytes, CancellationToken.None);
                    }
                }
            }

            _logger.LogWarning("forwardAccept finisehd");
        });

        var copy = Task.Run(async () =>
        {
            var buff = new byte[4096];
            while (true)
            {
                try
                {
                    var read = await streamOutput.ReadAsync(buff, 0, 4096);
                    if (read > 0)
                    {
                        await chOut.Writer.WriteAsync(buff.Take(read).ToArray(), pumpCancellation.Token);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("copy err:{0}", e);
                    break;
                }

            }
        });
        var forwardCopy = Task.Run(async () =>
        {
            while (await chOut.Reader.WaitToReadAsync(pumpCancellation.Token))
            {
                if (chOut.Reader.TryRead(out var arraySegment))
                {
                    var line = Encoding.UTF8.GetString(arraySegment);
                    _logger.LogInformation("send to client:{0}", line);
                    var obj = new
                    {
                        Type = "stdOut",
                        Output = line
                    };
                    var jsonStr = JSON.SerializeDynamic(obj, new Jil.Options(serializationNameFormat: SerializationNameFormat.CamelCase));

                    await clientWebSocket
                        .SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonStr ?? "")),
                            WebSocketMessageType.Text,
                            WebSocketMessageFlags.EndOfMessage,
                            pumpCancellation.Token
                        );
                }
            }

            _logger.LogWarning("forwardAccept finisehd");
        });


        await Task.WhenAny(accept, copy);
        pumpCancellation.Cancel(false);
        chIn.Writer.Complete();
        chOut.Writer.Complete();

        demux.Dispose();
        streamIn.Dispose();
        streamOutput.Dispose();
        resizeStream.Dispose();

        if (webSocket.State == WebSocketState.Open
               ||
               webSocket.State == WebSocketState.CloseReceived
               || webSocket.State == WebSocketState.CloseSent)
        {
            try
            {
                await webSocket.CloseAsync(
                   WebSocketCloseStatus.NormalClosure,
                   "disconnected",
                   CancellationToken.None
               );
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        //await forwardAccept;
        //await forwardCopy;

        _logger.LogWarning("finished");
    }


    private async Task Proxy4(WebSocket clientWebSocket, TerminalInfo info)
    {
        var kubernetes = _k8sFactory.MustGet(info.PodName);

        var chIn = Channel.CreateUnbounded<byte[]>();
        var chOut = Channel.CreateUnbounded<byte[]>();

        var pumpCancellation = new CancellationTokenSource();

        var handler = new ExecAsyncCallback(async (stdIn, stdOut, stdError) =>
        {
            var copy = Task.Run(async () =>
            {
                var buff = new byte[4096];
                while (true)
                {
                    try
                    {
                        var read = await stdOut.ReadAsync(buff, 0, 4096);

                        var line = Encoding.UTF8.GetString(buff.Take(read).ToArray());
                        _logger.LogInformation("send to client:{0}", line);
                        var obj = new
                        {
                            Type = "stdOut",
                            Output = line
                        };
                        var jsonStr = JSON.SerializeDynamic(obj, new Jil.Options(serializationNameFormat: SerializationNameFormat.CamelCase));

                        await clientWebSocket
                            .SendAsync(
                                new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonStr ?? "")),
                                WebSocketMessageType.Text,
                                WebSocketMessageFlags.EndOfMessage,
                                pumpCancellation.Token
                            );
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("copy err:{0}", e);
                        break;
                    }
                }
            });

            var accept = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var clientBuf = new byte[4096];
                        var podBuf = new byte[4096];

                        var receiveResult = await clientWebSocket.ReceiveAsync(
                            new ArraySegment<byte>(clientBuf), CancellationToken.None);

                        while (!receiveResult.CloseStatus.HasValue)
                        {
                            var arraySegment = new ArraySegment<byte>(
                                clientBuf.Take(receiveResult.Count).ToArray()
                            );

                            var inputObj = Encoding.UTF8.GetString(arraySegment);
                            _logger.LogInformation("receive client:{0}", inputObj);

                            var jsonObj = JSON.DeserializeDynamic(inputObj);
                            if (jsonObj.type == "stdIn")
                            {
                                string input = jsonObj.input;
                                var bytes = Encoding.UTF8.GetBytes(input);
                                await stdIn.WriteAsync(bytes, pumpCancellation.Token);
                            }
                            else if (jsonObj.type == "resize")
                            {
                                //int rows = jsonObj.rows;
                                //int cols = jsonObj.cols;
                                //var whInfo = new
                                //{
                                //    Width = cols,
                                //    Height = rows,
                                //};
                                //var jsonStr = JSON.SerializeDynamic(whInfo);
                                //jsonStr = "{\"Height\":24,\"Width\":159}";
                                //var bytes = Encoding.UTF8.GetBytes(jsonStr);
                                //await resizeStream.WriteAsync(bytes, CancellationToken.None);
                            }

                            receiveResult = await clientWebSocket.ReceiveAsync(
                              new ArraySegment<byte>(clientBuf), pumpCancellation.Token);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("accept err:{0}", e);
                        break;
                    }
                }
            });

            await Task.WhenAny(accept, copy);
            pumpCancellation.Cancel(false);

            if (clientWebSocket.State == WebSocketState.Open
              ||
              clientWebSocket.State == WebSocketState.CloseReceived
              || clientWebSocket.State == WebSocketState.CloseSent)
            {
                try
                {
                    await clientWebSocket.CloseAsync(
                       WebSocketCloseStatus.NormalClosure,
                       "disconnected",
                       CancellationToken.None
                   );
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            _logger.LogWarning("finished");
        });

        // https://github.com/kubernetes-client/csharp/issues/949

        try
        {
            var webSocket =
            await kubernetes.NamespacedPodExecAsync(
                    info.PodName,
                    info.NsName,
                    info.ContainerName,
                    new List<string>()
                    {
                            "/bin/bash"
                    },
                    true,
                    handler,
                    pumpCancellation.Token
                )
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task Proxy5(WebSocket clientWebSocket, TerminalInfo info)
    {
        var chIn = Channel.CreateUnbounded<byte[]>();
        var chOut = Channel.CreateUnbounded<byte[]>();

        var pumpCancellation = new CancellationTokenSource();

        Func<Stream, Stream, Stream, Stream, Task> handler =
            async (stdIn, stdOut, stdError, resizeStream) =>
            {
                {
                    var buff = new byte[4096];
                    var read = await stdOut.ReadAsync(buff, 0, buff.Length);
                    if (read == 0)
                    {
                        throw new InvalidParamterException("not valid exec cmd");
                    }
                }
                var copy = Task.Run(async () =>
                {
                    var buff = new byte[4096];
                    while (true)
                    {
                        try
                        {
                            var read = await stdOut.ReadAsync(buff, 0, buff.Length);
                            var line = Encoding.UTF8.GetString(buff.Take(read).ToArray());
                            _logger.LogInformation("send to client:{0}", line);
                            var obj = new
                            {
                                Type = "stdOut",
                                Output = line
                            };
                            var jsonStr = JSON.SerializeDynamic(obj, new Jil.Options(serializationNameFormat: SerializationNameFormat.CamelCase));

                            await clientWebSocket
                                    .SendAsync(
                                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonStr ?? "")),
                                        WebSocketMessageType.Text,
                                        WebSocketMessageFlags.EndOfMessage,
                                        pumpCancellation.Token
                                    );
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("copy err:{0}", e);
                            break;
                        }
                    }
                });

                var accept = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            var clientBuf = new byte[4096];
                            var podBuf = new byte[4096];

                            var receiveResult = await clientWebSocket.ReceiveAsync(
                                    new ArraySegment<byte>(clientBuf), CancellationToken.None);

                            while (!receiveResult.CloseStatus.HasValue)
                            {
                                var arraySegment = new ArraySegment<byte>(
                                        clientBuf.Take(receiveResult.Count).ToArray()
                                    );

                                var inputObj = Encoding.UTF8.GetString(arraySegment);
                                _logger.LogInformation("receive client:{0}", inputObj);

                                var jsonObj = JSON.DeserializeDynamic(inputObj);
                                if (jsonObj.type == "stdIn")
                                {
                                    string input = jsonObj.input;
                                    var bytes = Encoding.UTF8.GetBytes(input);
                                    await stdIn.WriteAsync(bytes, pumpCancellation.Token);
                                }
                                else if (jsonObj.type == "resize")
                                {
                                    int rows = jsonObj.rows;
                                    int cols = jsonObj.cols;
                                    var whInfo = new
                                    {
                                        Width = cols,
                                        Height = rows,
                                    };
                                    var jsonStr = JSON.SerializeDynamic(whInfo);
                                    var bytes = Encoding.UTF8.GetBytes(jsonStr);
                                    await resizeStream.WriteAsync(bytes, CancellationToken.None);
                                }

                                receiveResult = await clientWebSocket.ReceiveAsync(
                                      new ArraySegment<byte>(clientBuf), pumpCancellation.Token);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("accept err:{0}", e);
                            break;
                        }
                    }
                });

                await Task.WhenAny(accept, copy);
                pumpCancellation.Cancel(false);

                stdIn.Close();
                stdOut.Close();
                resizeStream.Close();

                if (clientWebSocket.State == WebSocketState.Open
                      ||
                      clientWebSocket.State == WebSocketState.CloseReceived
                      || clientWebSocket.State == WebSocketState.CloseSent)
                {
                    try
                    {
                        await clientWebSocket.CloseAsync(
                               WebSocketCloseStatus.NormalClosure,
                               "disconnected",
                               CancellationToken.None
                           );
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                _logger.LogWarning("finished");
            };

        // https://github.com/kubernetes-client/csharp/issues/949

        foreach (var cmd in ValidBashs)
        {
            try
            {
                var exitCode = await NamespacedPodExecAsync(
                          info.PodName,
                          info.NsName,
                          info.ContainerName,
                          new List<string>()
                          {
                                cmd
                          },
                          true,
                          handler,
                          pumpCancellation.Token
                      )
                      .ConfigureAwait(false);
                Console.WriteLine($"exitCode: {exitCode}");
                break;
            }
            catch (InvalidParamterException)
            {
                Console.WriteLine($"InvalidCmd:{cmd}");
                continue;
            }
        }
    }

    /// <summary>
    /// https://github.com/kubernetes-client/csharp/blob/3702fd6e904869ad6ec239c956d3d26567851da5/src/KubernetesClient/Kubernetes.Exec.cs#L11
    /// </summary>
    /// <param name="name"></param>
    /// <param name="namespace"></param>
    /// <param name="container"></param>
    /// <param name="command"></param>
    /// <param name="tty"></param>
    /// <param name="action"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="KubernetesException"></exception>

    private async Task<int> NamespacedPodExecAsync(
            string name,
            string @namespace,
            string container,
            IEnumerable<string> command,
            bool tty,
            Func<Stream, Stream, Stream, Stream, Task> handler,
            CancellationToken cancellationToken
     )
    {
        var kubernetes = _k8sFactory.MustGet(name);

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        try
        {
            using var muxedStream = await kubernetes.MuxedStreamNamespacedPodExecAsync(
                name,
                @namespace,
                command,
                container,
                tty: tty,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            using var stdIn = muxedStream.GetStream(null, ChannelIndex.StdIn);
            using var stdOut = muxedStream.GetStream(ChannelIndex.StdOut, null);
            using var stdErr = muxedStream.GetStream(ChannelIndex.StdErr, null);
            using var error = muxedStream.GetStream(ChannelIndex.Error, null);
            using var resizeStream = muxedStream.GetStream(null, ChannelIndex.Resize);
            using var errorReader = new StreamReader(error);

            muxedStream.Start();

            await handler(stdIn, stdOut, stdErr, resizeStream)
                .ConfigureAwait(false);
            var errors = await errorReader.ReadToEndAsync();
            var returnMessage = KubernetesJson.Deserialize<V1Status>(errors);

            //V1Status? returnMessage = null;
            //try
            //{
            //    var errors = await errorReader.ReadToEndAsync()
            //        .WaitAsync(TimeSpan.FromSeconds(5))
            //        .ConfigureAwait(false);
            //    returnMessage = KubernetesJson.Deserialize<V1Status>(errors);
            //}
            //catch (TimeoutException)
            //{
            //    // ignore
            //}

            return returnMessage == null ? 0 : Kubernetes.GetExitCodeOrThrow(returnMessage);
        }
        catch (HttpOperationException httpEx) when (httpEx.Body is V1Status status)
        {
            throw new KubernetesException(status, httpEx);
        }
    }
}

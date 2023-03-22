import { Terminal } from "xterm";
import { FitAddon } from "xterm-addon-fit";
// @ts-ignore
import { AttachAddon } from "xterm-addon-attach";  // "./AttachAddon"

var term: Terminal,
    socket: WebSocket;
var _initialized = false;
// @ts-ignore
const shellprompt = "$ ";

export default function initTerminal(
    ele: HTMLElement,
    serverUri: string,
) {
    term = new Terminal({
        cursorBlink: true,
    });

    var cols = Math.floor(ele.clientWidth / 9);
    var rows = Math.floor(ele.clientHeight / 18);
    term.resize(cols, rows);

    (window as any).term = term;
    term.open(ele);
    const fitAddon = new FitAddon();
    term.loadAddon(fitAddon);
    fitAddon.fit();

    // 当浏览器窗口变化时, 重新适配终端
    // @ts-ignore
    term.onResize((info, dv) => {
        console.log("resize info, dv", info)
        const { cols, rows } = info;
        // 把web终端的尺寸term.rows和term.cols发给服务端, 通知sshd调整输出宽度
        var msg = { type: "resize", rows: rows, cols: cols };
        term.scrollToBottom();
        socket.send(JSON.stringify(msg));
    })
    window.addEventListener("resize", function () {
        fitAddon.fit();
    });
    // term.toggleFullscreen();

    console.debug(serverUri);
    createWs(serverUri);
    term.onData((key, ev) => {
        if (_initialized) {
            var msg = { type: "stdIn", input: key, ev: ev };
            socket.send(JSON.stringify(msg));
        }
    });

    //socket = new WebSocket(serverUri);
    //const attachAddon = new AttachAddon(socket, {
    //    bidirectional: true
    //});
    //term.loadAddon(attachAddon);

    
}

function createWs(serverUri: string) {
    let ws = new WebSocket(serverUri);
    ws.onopen = () => {
        _initialized = true;
        term.options.disableStdin = false;
        var msg = { type: "resize", rows: term.rows, cols: term.cols };
        socket.send(JSON.stringify(msg));
    }
    ws.onmessage = (msg) => {
        console.debug("onmessage", msg.data);
        const msgObj = JSON.parse(msg.data);
        if (msgObj.type === "stdOut") {
            var output: string = msgObj.output;
            term.write(output);
        }
    };
    ws.onclose = () => {
        term.write("\r\nConnection has been disconnected!");
        _initialized = false;
        term.options.disableStdin = true;
    };
    ws.onerror = () => {
        term.write("\r\nConnection has been disconnected!");
        _initialized = false;
        term.options.disableStdin = true;
    };

    socket = ws;
}


import "./CreateApp.scss";
import "./CreateSvcForm.scss";
import "./CreateIng.scss";

import { Terminal } from "xterm";
import { FitAddon } from "xterm-addon-fit";
// @ts-ignore
import { AttachAddon } from "xterm-addon-attach";  // "./AttachAddon"
import ReconnectingWebSocket from "reconnecting-websocket";

var term: Terminal, socket: ReconnectingWebSocket;
var _initialized = false;

// @ts-ignore
function runRealTerminal() {
    console.log("runRealTerminal");
    // @ts-ignore
    // var attachAddon = new AttachAddon(socket);
    // term.loadAddon(attachAddon);
    _initialized = true;
}

const shellprompt = "$ ";
let command = "";
const promptTerm = function () {
    command = "";
    term.write("\r\n" + shellprompt);
};

function runFakeTerminal() {
    if (_initialized) {
        return;
    }

    _initialized = true;

    term.writeln("Welcome to xterm.js");
    term.writeln(
        "This is a local terminal emulation, without a real terminal in the back-end."
    );
    term.writeln("Type some keys and commands to play around.");
    term.writeln("");
    promptTerm();
}

function runCommand(term: Terminal, text: string) {
    const command = text.trim().split(" ")[0];
    if (command.length > 0) {
        term.writeln("");
        console.log("command:", command);
        //socket.send(command + "\n")
    }
    promptTerm();
}

// @ts-ignore
const onKey = (keyInfo: { key: string, domEvent: KeyboardEvent }) => {
    // @ts-ignore
    const { key, domEvent } = keyInfo;
    console.log(keyInfo);
    switch (key) {
        case "\x03": // Ctrl+C
            term.write("^C");
            promptTerm();
            break;
        case "\r": // Enter
            runCommand(term, command);
            command = "";
            break;
        case "\x7F": // Backspace (DEL)
            // Do not delete the prompt
            // @ts-ignore
            if (term._core.buffer.x > 2) {
                term.write("\b \b");
                if (command.length > 0) {
                    command = command.substr(0, command.length - 1);
                }
            }
            break;
        default: // Print all other characters for demo
            if (
                (key >= String.fromCharCode(0x20) &&
                    key <= String.fromCharCode(0x7e)) ||
                key >= "\u00a0"
            ) {
                command += key;
                //   term.write(key);
            }
    }
}

function initTerminal(
    ele: HTMLElement,
    iamId: number,
    podName: string,
    containerName: string
) {
    //var ele = document.getElementById('terminal');
    term = new Terminal({
        cursorBlink: true,
    });

    var cols = Math.floor(ele.clientWidth / 9);
    var rows = Math.floor(ele.clientHeight / 18);
    console.log("cols, rows", cols, rows)
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
        // var cols =  Math.floor(ele.clientWidth/9);
        // var rows = Math.floor(ele.clientHeight/18);
        // console.log("resize cols, rows", cols, rows)
        // term.resize(cols, rows);
        // // 把web终端的尺寸term.rows和term.cols发给服务端, 通知sshd调整输出宽度
        // var msg = { type: "resize", rows: term.rows, cols: term.cols };
        // term.scrollToBottom();
        // socket.send(JSON.stringify(msg));
    });

    // term.toggleFullscreen();

    var protocol = location.protocol === "https:" ? "wss://" : "ws://";
    var serverUri = `${protocol}127.0.0.1:5125/api/Deploy/Terminal/${iamId}/${podName}/${containerName}`;

    socket = new ReconnectingWebSocket(serverUri, [], {
        debug: true,
        connectionTimeout: 100,
    });

    socket.onopen = () => {
        _initialized = true;
        console.log(term.cols);
        console.log(term.rows);
        var msg = { type: "resize", rows: term.rows, cols: term.cols };
        socket.send(JSON.stringify(msg));
    };
    socket.onmessage = (msg) => {
        console.debug("onmessage", msg.data);
        const msgObj = JSON.parse(msg.data);
        if (msgObj.type === "stdOut") {
            var output: string = msgObj.output;
            term.write(output);
        }
    };

    socket.onclose = runFakeTerminal;
    socket.onerror = runFakeTerminal;

    term.onData((key, ev) => {
        var msg = { type: "stdIn", input: key, ev: ev };
        socket.send(JSON.stringify(msg));
    });
}

(window as any).initTerminal = initTerminal;
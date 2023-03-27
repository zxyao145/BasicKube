import "./CreateApp.scss";
import "./CreateSvcForm.scss";
import "./CreateIng.scss";
import "./JobEditForm.scss";

import initTerminal from "./Terminal"

(window as any).initTerminal = initTerminal;

(window as any).readCookie = (cname: string) => {
    var name = cname + "=";
    var decodedCookie = decodeURIComponent(document.cookie);
    var ca = decodedCookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}
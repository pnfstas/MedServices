if(typeof console === "undefined" || console === null)
{
    Object.assign(console,
    {
        log: (msg) => alert(msg),
        info: (msg) => alert(msg),
        warn: (msg) => alert(msg),
        error: (msg) => alert(msg),
        debug: (msg) => alert(msg),
        trace: (...data) => alert((new Error).stack)
    });
}
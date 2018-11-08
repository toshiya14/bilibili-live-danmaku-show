var express = require('express');
var path = require('path');
var bodyparser = require('body-parser');
var app = express();
var cache = [];

var genMsg = function (msg) {
    var time = Date.now();
    var type = msg.type || 'SYSTEM';
    var msg = msg;
    return {
        time: time,
        type: type,
        msg: msg
    }
}

app.use("/css", express.static(path.join(__dirname, 'css')));
app.use("/js", express.static(path.join(__dirname, 'js')));
app.use("/index.html", express.static(path.join(__dirname, 'index.html')));
app.use(bodyparser.json());

app.post("/danmaku", function (req, resp) {
    var data = req.body;
    if (data.ds && data.count > 0) {
        data.ds.map(function (e) {
            cache.push(genMsg(e))
        });
    } else {
        cache.push(genMsg(data));
    }
    if (cache.length > 100) {
        cache.splice(0, cache.length - 100);
    }
    resp.json({
        msg: "OK"
    });
});

app.get("/danmaku", function (req, resp) {
    var data = req.query;
    var lasttime = data.lt;
    var list = cache.filter(e => e.time >= lasttime);
    resp.json({
        time: Date.now(),
        cnt: list.length,
        ds: list
    });
});

app.get("/lastdm", function (req, resp) {
    var data = req.query;
    var count = data.c;
    var list = cache.slice(-count);
    resp.json({
        time: Date.now(),
        cnt: list.length,
        ds: list
    });
});

app.get("/time", function (req, resp) {
    resp.json({
        time: Date.now()
    });
});

app.get("/ping", function (req, resp) {
    resp.send("bilibili-show-pong#Akaishi")
});

app.listen(6099);
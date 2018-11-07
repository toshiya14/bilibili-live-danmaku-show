var SCAN_FREQUENCE = 500;
var MAX_DANMAKU = 3;
var ICO_DURATION = 800;
var NAME_DURATION = 800;
var CHAR_DURATION = 100;
var last_fetch_time = 0;
var renderState = 'free';
var danlist = [];

$(function () {
    var showComment = function (ico, name, msg) {
        var ico = ico || 'ico-danmaku';
        var name = name || '';
        var msg = msg || '';

        renderState = 'busy';
        var $el = $(".danmaku-board");

        var $li = $('<li class="danmaku-item"></li>');
        $el.append($li);
        var $ico = $('<span class="ico ' + ico + '"></div>');
        $li.append($ico);
        $ico.addClass("active");
        var $name = $('<span class="name"></div>');

        $name.addClass("active");
        $name.removeClass("active");
        setTimeout(function () {
            $li.append($name);
            $name.html(name);
            $name.addClass("active");
        }, ICO_DURATION * 0.9);

        // split msg
        var charlist = msg.split("").map(x => '<span class="char">' + htmlencode(x) + '</span>');
        if (charlist.length === 0) {
            setTimeout(function () {
                renderState = 'free';
            }, ICO_DURATION + NAME_DURATION);
        }
        var ci = 0;
        var addChar = function ($chr, time, ci) {
            setTimeout(() => {
                $li.append($chr);
                $chr.addClass("active");
                if (ci === charlist.length - 1) {
                    setTimeout(function () {
                        renderState = 'free';
                    }, 500);
                }
            }, time);
        };
        charlist.map(function (chr) {
            $chr = $(chr);
            var time = ICO_DURATION + NAME_DURATION + (ci * CHAR_DURATION) - CHAR_DURATION * 0.5;
            addChar($chr, time, ci);
            ci++;
        });
    }
    var dismissComment = function (el) {
        var $el = $(el);
        $el.addClass("predelete");
        var $items = $el.children("span");
        $items.each(function () {
            $this = $(this);
            var $tmp = $this.clone();
            $this.replaceWith($tmp.removeClass("active").addClass("predelete"));
        });
        $el.css({
            'overflow': 'hidden'
        })
        $el.animate({
            height: 0,
            margin: 0,
            padding: 0,
            opacity: 0
        }, 700).promise().then(function () {
            $el.remove();
        });
    }
    var htmlencode = function (text) {
        if (text === " ") {
            return "&nbsp;";
        }
        var div = $("<div>" + text + "</div>");
        var r = div.html();
        div.remove();
        return r;
    }
    var processComment = function (e) {
        var item = {};
        if (e.type === 'SYSTEM') {
            item.ico = 'ico-system',
                item.name = e.msg.msg;
        } else if (e.type === 'GIFT') {
            item.ico = 'ico-gift',
                item.name = e.msg.data.user;
            item.msg = e.msg.data.gift + " x " + e.msg.data.count;
        } else if (e.type === 'DANMAKU') {
            item.ico = e.msg.data.isAdmin ? 'ico-admin' : e.msg.data.isVIP ? 'ico-vip' : 'ico-danmaku';
            item.name = e.msg.data.user;
            item.msg = e.msg.data.text;
        } else if (e.type === 'WELCOME') {
            item.ico = e.msg.data.isAdmin ? 'ico-admin' : e.msg.data.isVIP ? 'ico-vip' : 'ico-danmaku';
            item.name = e.msg.data.user;
            item.msg = "进入房间";
        } else {
            return undefined;
        }
        return item;
    }
    var fetchComments = function () {
        $.getJSON({
            dataType: 'json',
            url: '/danmaku?lt=' + last_fetch_time,
        }).done(function (resp) {
            last_fetch_time = resp.time;
            if (resp.cnt > 0) {
                resp.ds.map(function (e) {
                    var item = processComment(e);
                    if (item) {
                        danlist.push(item);
                    }
                });
            }
        });
    }
    var processQueue = function () {
        if (renderState === 'busy') {
            setTimeout(processQueue, SCAN_FREQUENCE);
        } else {
            var $danitems = $(".danmaku-board").children(":not(.predelete)");
            if ($danitems.length > MAX_DANMAKU) {
                var $el = $($danitems[0]);
                dismissComment($el);
            }
            var item = danlist.splice(0, 1)[0];
            if (item) {
                showComment(item.ico, item.name, item.msg);
            }
            setTimeout(processQueue, SCAN_FREQUENCE);
        }
    }
    processQueue();
    $.getJSON({
        dataType: 'json',
        url: '/lastdm?c=5'
    }).done(function (resp) {
        last_fetch_time = resp.time;
        if (resp.cnt > 0) {
            resp.ds.map(function (e) {
                var item = processComment(e);
                if (item) {
                    danlist.push(item);
                }
            });
        }
    });
    setInterval(() => {
        if (last_fetch_time === 0) {
            return;
        }
        fetchComments();
    }, 200);
});
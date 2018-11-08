@echo off
if exist danmaku-show-server.tgz (
	npm install danmaku-show-server.tgz
	cd node_modules/danmaku-show-server/
	node main.js
) else (
	REM Nothing
)
#!/bin/bash
# Manage daily recap daemon

DAEMON_SCRIPT="/home/user/Capstone-FinalArc/.claude/daily-recap-daemon.sh"
PID_FILE="/tmp/daily-recap-daemon.pid"
LOG_FILE="/tmp/daily-recap-daemon.log"

case "$1" in
  start)
    if [ -f "$PID_FILE" ] && kill -0 $(cat "$PID_FILE") 2>/dev/null; then
      echo "❌ Daemon already running (PID: $(cat $PID_FILE))"
    else
      nohup "$DAEMON_SCRIPT" > "$LOG_FILE" 2>&1 &
      echo $! > "$PID_FILE"
      echo "✅ Daemon started (PID: $(cat $PID_FILE))"
    fi
    ;;
  stop)
    if [ -f "$PID_FILE" ]; then
      kill $(cat "$PID_FILE") 2>/dev/null && echo "✅ Daemon stopped" || echo "❌ Failed to stop daemon"
      rm "$PID_FILE"
    else
      echo "❌ Daemon not running"
    fi
    ;;
  status)
    if [ -f "$PID_FILE" ] && kill -0 $(cat "$PID_FILE") 2>/dev/null; then
      echo "✅ Daemon running (PID: $(cat $PID_FILE))"
      echo "Log: tail -f $LOG_FILE"
    else
      echo "❌ Daemon not running"
    fi
    ;;
  log)
    tail -f "$LOG_FILE"
    ;;
  *)
    echo "Usage: $0 {start|stop|status|log}"
    ;;
esac

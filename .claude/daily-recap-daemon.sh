#!/bin/bash
# Daily recap daemon - runs at 9 AM WIB every day
# This script runs in the background and checks the time every minute

RECAP_SCRIPT="/home/user/Capstone-FinalArc/.claude/daily-recap.py"
LOCK_FILE="/home/user/Capstone-FinalArc/.claude/.recap-lock"
LOG_FILE="/var/log/daily-recap.log"

while true; do
    # Get current hour in WIB (UTC+7)
    CURRENT_HOUR_UTC=$(date -u +%H)
    CURRENT_HOUR_WIB=$(( (CURRENT_HOUR_UTC + 7) % 24 ))
    CURRENT_DATE=$(date +%Y-%m-%d)
    CURRENT_TIME=$(date "+%Y-%m-%d %H:%M:%S")

    # Check if it's 9 AM WIB (hour = 09)
    if [ "$CURRENT_HOUR_WIB" -eq 9 ]; then
        # Check if we've already run today
        if [ ! -f "$LOCK_FILE" ] || [ "$(cat "$LOCK_FILE")" != "$CURRENT_DATE" ]; then
            echo "[$CURRENT_TIME] Running daily recap..." >> "$LOG_FILE"
            python3 "$RECAP_SCRIPT" >> "$LOG_FILE" 2>&1

            # Update lock file
            mkdir -p "$(dirname "$LOCK_FILE")"
            echo "$CURRENT_DATE" > "$LOCK_FILE"

            echo "[$CURRENT_TIME] Daily recap completed" >> "$LOG_FILE"
        fi
    fi

    # Check every minute
    sleep 60
done

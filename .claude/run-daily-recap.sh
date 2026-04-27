#!/bin/bash
# Wrapper script to run daily recap at 9 AM WIB (2 AM UTC)
# This runs on SessionStart to check if it's time to generate the recap

REPO_PATH="/home/user/Capstone-FinalArc"
RECAP_SCRIPT="$REPO_PATH/.claude/daily-recap.py"
LOCK_FILE="$REPO_PATH/.claude/.recap-lock"

# Get current hour in WIB (UTC+7)
# System is in UTC, so add 7 hours
CURRENT_HOUR_UTC=$(date -u +%H)
CURRENT_HOUR_WIB=$(( (CURRENT_HOUR_UTC + 7) % 24 ))
CURRENT_DATE=$(date +%Y-%m-%d)

# Check if it's 9 AM WIB (hour = 09)
if [ "$CURRENT_HOUR_WIB" -eq 9 ]; then
    # Check if we've already run today
    if [ ! -f "$LOCK_FILE" ] || [ "$(cat "$LOCK_FILE")" != "$CURRENT_DATE" ]; then
        # Run the recap script
        python3 "$RECAP_SCRIPT" 2>&1 | tail -20

        # Update lock file with today's date
        mkdir -p "$(dirname "$LOCK_FILE")"
        echo "$CURRENT_DATE" > "$LOCK_FILE"
    fi
fi

#!/usr/bin/env python3
"""
Daily GitHub Commit Recap - Auto-posts to Notion every day at 9 AM WIB (2 AM UTC)
"""

import subprocess
import json
import requests
from datetime import datetime, timedelta
import os
import sys

# Configuration
NOTION_API_KEY = "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"
NOTION_PAGE_ID = "2694045038d580258c5dcacb33f610fc"
REPO_PATH = "/home/user/Capstone-FinalArc"

NOTION_API_URL = "https://api.notion.com/v1"
NOTION_VERSION = "2022-06-28"

headers = {
    "Authorization": f"Bearer {NOTION_API_KEY}",
    "Content-Type": "application/json",
    "Notion-Version": NOTION_VERSION,
}


def get_yesterday_commits():
    """Fetch commits from yesterday, excluding merge commits"""
    os.chdir(REPO_PATH)

    today = datetime.utcnow().date()
    yesterday = today - timedelta(days=1)

    # Get commits from yesterday (UTC timestamps)
    cmd = [
        "git", "log",
        f"--since={yesterday} 00:00:00",
        f"--until={today} 00:00:00",
        "--oneline",
        "--no-merges"
    ]

    result = subprocess.run(cmd, capture_output=True, text=True)
    commits = result.stdout.strip().split('\n') if result.stdout.strip() else []

    return commits, yesterday


def get_commit_details(commit_hash):
    """Get detailed info about a commit"""
    os.chdir(REPO_PATH)

    cmd = ["git", "show", commit_hash, "--stat", "--format=%B"]
    result = subprocess.run(cmd, capture_output=True, text=True)

    # Also get the full diff summary
    cmd_diff = ["git", "show", commit_hash, "--stat"]
    result_diff = subprocess.run(cmd_diff, capture_output=True, text=True)

    return result.stdout, result_diff.stdout


def format_recap(commits_data, date):
    """Format commits into Notion-compatible markdown"""
    date_str = date.strftime("%B %d, %Y")

    if not commits_data:
        recap = f"## 📅 Daily Recap — {date_str}\n\n**Total Commits:** 0 (No activity)\n\n---"
        return recap

    recap = f"## 📅 Daily Recap — {date_str}\n\n**Total Commits:** {len(commits_data)}\n\n"

    for i, (commit_hash, message, stats) in enumerate(commits_data, 1):
        recap += f"### Commit {i}: {message}\n"
        recap += f"**Hash:** `{commit_hash[:7]}`\n\n"
        recap += f"**Changes:**\n{stats}\n\n"

    recap += "---\n\n**✂️ Separator**\n\n---\n\n"

    return recap


def update_notion_page(recap_content):
    """Append recap content to the top of the Notion page"""
    url = f"{NOTION_API_URL}/pages/{NOTION_PAGE_ID}"

    try:
        # Get current page content
        response = requests.get(url, headers=headers, timeout=10)

        if response.status_code != 200:
            print(f"Error fetching page: {response.status_code}")
            if response.status_code == 403:
                print("⚠️ Notion API access denied. Check API key and page permissions.")
            print(response.text)
            return False
    except requests.exceptions.RequestException as e:
        print(f"Network error: {e}")
        return False

    # Update page with new content prepended
    blocks_url = f"{NOTION_API_URL}/blocks/{NOTION_PAGE_ID}/children"

    # Create block with recap content
    new_block = {
        "children": [
            {
                "object": "block",
                "type": "paragraph",
                "paragraph": {
                    "rich_text": [
                        {
                            "type": "text",
                            "text": {
                                "content": recap_content,
                                "link": None
                            }
                        }
                    ]
                }
            }
        ]
    }

    response = requests.patch(blocks_url, headers=headers, json=new_block)

    if response.status_code != 200:
        print(f"Error updating page: {response.status_code}")
        print(response.text)
        return False

    return True


def main():
    print("🔄 Starting Daily Recap Generation...")

    commits, yesterday = get_yesterday_commits()

    if not commits or commits == ['']:
        print(f"✅ No commits found for {yesterday}")
        recap = f"## 📅 Daily Recap — {yesterday.strftime('%B %d, %Y')}\n\n**Total Commits:** 0 (No activity)\n\n---"
    else:
        print(f"📊 Found {len(commits)} commits from {yesterday}")

        commits_data = []
        for commit_line in commits:
            if not commit_line.strip():
                continue

            parts = commit_line.split(maxsplit=1)
            commit_hash = parts[0]
            message = parts[1] if len(parts) > 1 else "No message"

            # Get commit stats
            details, stats = get_commit_details(commit_hash)
            commits_data.append((commit_hash, message, stats))

        recap = format_recap(commits_data, yesterday)
        print(f"📝 Formatted recap:\n{recap[:200]}...")

    # Update Notion
    print("📤 Uploading to Notion...")
    if update_notion_page(recap):
        print("✅ Successfully updated Notion page!")
    else:
        print("❌ Failed to update Notion page")
        sys.exit(1)


if __name__ == "__main__":
    main()

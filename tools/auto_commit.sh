#!/bin/sh
# Dùng: tools/auto_commit.sh "<type>(<scope>): <mô tả>"
set -e
cd "$(dirname "$0")/.."
git add -A
if git diff --cached --quiet; then
  echo "Không có thay đổi để commit."; exit 0
fi
git commit -m "$1"

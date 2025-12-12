#!/bin/bash

# Script to delete Dev branch and create Develop branch from main
# This script should be run by someone with appropriate repository permissions

set -e  # Exit on error

REPO_DIR="/home/runner/work/Middleware_RESTApi_v1/Middleware_RESTApi_v1"
cd "$REPO_DIR"

echo "=========================================="
echo "Branch Management Script"
echo "=========================================="
echo ""

# Fetch latest changes
echo "Step 1: Fetching latest changes from remote..."
git fetch origin '+refs/heads/*:refs/remotes/origin/*'
echo "✓ Fetch complete"
echo ""

# Checkout main branch
echo "Step 2: Checking out main branch..."
if git show-ref --verify --quiet refs/heads/main; then
    git checkout main
    git pull origin main
else
    git checkout -b main origin/main
fi
echo "✓ Main branch ready"
echo ""

# Create Develop branch from main
echo "Step 3: Creating Develop branch from main..."
if git show-ref --verify --quiet refs/heads/Develop; then
    echo "! Develop branch already exists locally, checking it out..."
    git checkout Develop
else
    git checkout -b Develop
fi
echo "✓ Develop branch created"
echo ""

# Push Develop branch to remote
echo "Step 4: Pushing Develop branch to remote..."
git push -u origin Develop
echo "✓ Develop branch pushed to remote"
echo ""

# Delete Dev branch from remote
echo "Step 5: Deleting Dev branch from remote..."
git push origin --delete Dev
echo "✓ Dev branch deleted from remote"
echo ""

# Clean up local Dev branch if it exists
if git show-ref --verify --quiet refs/heads/Dev; then
    echo "Step 6: Cleaning up local Dev branch..."
    git checkout Develop  # Make sure we're not on Dev
    git branch -D Dev
    echo "✓ Local Dev branch deleted"
    echo ""
fi

echo "=========================================="
echo "Branch management completed successfully!"
echo "=========================================="
echo ""
echo "Summary:"
echo "  ✓ Develop branch created from main"
echo "  ✓ Develop branch pushed to remote"
echo "  ✓ Dev branch deleted from remote"
echo ""
echo "Verification:"
git branch -r | grep -E "(Develop|Dev|main)" || true

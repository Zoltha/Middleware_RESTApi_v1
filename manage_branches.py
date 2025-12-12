#!/usr/bin/env python3
"""
Script to delete Dev branch and create Develop branch from main using GitHub API.
Requires: pip install PyGithub
Usage: python manage_branches.py <github_token>
"""

import sys
import os

def main():
    try:
        from github import Github
    except ImportError:
        print("Error: PyGithub not installed. Install it with: pip install PyGithub")
        sys.exit(1)
    
    # Get GitHub token from command line or environment
    token = None
    if len(sys.argv) > 1:
        token = sys.argv[1]
    else:
        token = os.environ.get('GITHUB_TOKEN')
    
    if not token:
        print("Error: GitHub token required.")
        print("Usage: python manage_branches.py <github_token>")
        print("Or set GITHUB_TOKEN environment variable")
        sys.exit(1)
    
    # Repository details
    repo_owner = "Zoltha"
    repo_name = "Middleware_RESTApi_v1"
    
    print(f"Connecting to GitHub...")
    g = Github(token)
    repo = g.get_repo(f"{repo_owner}/{repo_name}")
    
    print(f"Repository: {repo.full_name}")
    print("")
    
    # Step 1: Get the main branch's SHA
    print("Step 1: Getting main branch reference...")
    try:
        main_ref = repo.get_branch("main")
        main_sha = main_ref.commit.sha
        print(f"✓ Main branch SHA: {main_sha[:8]}...")
    except Exception as e:
        print(f"✗ Error getting main branch: {e}")
        sys.exit(1)
    
    print("")
    
    # Step 2: Create Develop branch from main
    print("Step 2: Creating Develop branch from main...")
    try:
        # Check if Develop already exists
        try:
            develop_ref = repo.get_git_ref("heads/Develop")
            print(f"! Develop branch already exists")
            print(f"  Current SHA: {develop_ref.object.sha[:8]}...")
            
            # Update it to point to main
            develop_ref.edit(main_sha)
            print(f"✓ Updated Develop to point to main ({main_sha[:8]}...)")
        except:
            # Create new Develop branch
            repo.create_git_ref(f"refs/heads/Develop", main_sha)
            print(f"✓ Created Develop branch from main ({main_sha[:8]}...)")
    except Exception as e:
        print(f"✗ Error creating Develop branch: {e}")
        sys.exit(1)
    
    print("")
    
    # Step 3: Delete Dev branch
    print("Step 3: Deleting Dev branch...")
    try:
        dev_ref = repo.get_git_ref("heads/Dev")
        dev_ref.delete()
        print(f"✓ Dev branch deleted")
    except Exception as e:
        if "Not Found" in str(e):
            print(f"! Dev branch not found (may already be deleted)")
        else:
            print(f"✗ Error deleting Dev branch: {e}")
            sys.exit(1)
    
    print("")
    print("=" * 50)
    print("Branch management completed successfully!")
    print("=" * 50)
    print("")
    
    # Verification
    print("Verification - Current branches:")
    branches = repo.get_branches()
    for branch in branches:
        if branch.name in ["main", "Develop", "Dev"]:
            print(f"  • {branch.name}: {branch.commit.sha[:8]}...")
    
    print("")
    print("Summary:")
    print("  ✓ Develop branch created/updated from main")
    print("  ✓ Dev branch deleted")

if __name__ == "__main__":
    main()

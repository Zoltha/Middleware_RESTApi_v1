# Branch Management Task: Delete Dev, Create Develop

## Overview

This PR contains scripts and documentation to help complete the task of:
1. Deleting the `Dev` branch
2. Creating a new `Develop` branch from `main`

## Current Branch Status

- **Dev branch**: Exists on remote, needs to be deleted
- **main branch**: Exists on remote, will be the source for Develop
- **Develop branch**: Needs to be created from main

## Execution Options

Choose one of the following methods to complete the task:

### Option 1: Shell Script (Recommended for local execution)

```bash
./manage_branches.sh
```

**Requirements:**
- Git installed and configured
- GitHub credentials with push/delete permissions
- Execute from repository root

### Option 2: Python Script with GitHub API

```bash
# Install dependency first
pip install PyGithub

# Run the script
python manage_branches.py YOUR_GITHUB_TOKEN
```

Or set the token as environment variable:

```bash
export GITHUB_TOKEN=your_token_here
python manage_branches.py
```

**Requirements:**
- Python 3.x
- PyGithub library (`pip install PyGithub`)
- GitHub Personal Access Token with `repo` scope

### Option 3: GitHub Web Interface (Manual)

Follow the step-by-step instructions in `BRANCH_MANAGEMENT_INSTRUCTIONS.md`

**Best for:** Users who prefer GUI or don't have local repository access

### Option 4: GitHub CLI (gh)

```bash
# Create Develop branch
gh api repos/Zoltha/Middleware_RESTApi_v1/git/refs \
  -f ref="refs/heads/Develop" \
  -f sha="$(gh api repos/Zoltha/Middleware_RESTApi_v1/git/refs/heads/main --jq .object.sha)"

# Delete Dev branch
gh api repos/Zoltha/Middleware_RESTApi_v1/git/refs/heads/Dev -X DELETE
```

**Requirements:**
- GitHub CLI installed and authenticated

## Verification

After executing any of the above methods, verify the changes:

```bash
# Check remote branches
git ls-remote --heads origin | grep -E "(Develop|Dev|main)"
```

Expected output should show:
- ✓ `main` branch exists
- ✓ `Develop` branch exists (newly created)
- ✗ `Dev` branch should NOT appear (deleted)

Or via GitHub web interface:
- Navigate to: https://github.com/Zoltha/Middleware_RESTApi_v1/branches
- Confirm `Develop` is listed
- Confirm `Dev` is NOT listed

## Files in This PR

| File | Purpose |
|------|---------|
| `manage_branches.sh` | Automated shell script for branch management |
| `manage_branches.py` | Python script using GitHub API |
| `BRANCH_MANAGEMENT_INSTRUCTIONS.md` | Detailed step-by-step instructions |
| `README_BRANCH_TASK.md` | This file - overview and execution guide |

## Troubleshooting

### Authentication Issues

If you encounter authentication errors:
- Ensure you have repository write permissions
- Check that your credentials/tokens are valid
- For git commands: `git config --list | grep user`
- For GitHub token: Verify it has `repo` scope

### Branch Already Exists

If `Develop` already exists:
- The Python script will update it to point to main
- The shell script will checkout existing branch
- Manual method: Delete existing Develop first, then recreate

### Dev Branch Not Found

If `Dev` branch doesn't exist:
- The task may already be partially complete
- The Python script will handle this gracefully
- Continue with creating Develop if needed

## Next Steps

After successful execution:
1. ✓ Verify branches using verification commands above
2. ✓ Update any CI/CD configurations that reference `Dev` to use `Develop`
3. ✓ Notify team members of the branch name change
4. ✓ Update any documentation that references the `Dev` branch

## Support

For issues or questions:
1. Check the detailed instructions in `BRANCH_MANAGEMENT_INSTRUCTIONS.md`
2. Review script comments in `manage_branches.sh` or `manage_branches.py`
3. Consult repository maintainers

---

**Note**: These scripts are provided as part of Copilot's automated PR creation. The actual branch operations require manual execution by someone with appropriate repository permissions.

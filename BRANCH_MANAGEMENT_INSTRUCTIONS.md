# Branch Management Instructions

This document contains instructions for managing branches in this repository.

## Task: Delete Dev Branch and Create Develop Branch

### Steps Required:

1. **Create new Develop branch from main**
   - The Develop branch should be based on the latest commit from the main branch
   - Base commit: The current HEAD of the main branch

2. **Delete the Dev branch**
   - The Dev branch should be removed from the remote repository
   - This can be done after the Develop branch is successfully created

### GitHub CLI Commands (for manual execution with proper credentials):

```bash
# Create Develop branch from main
git fetch origin main
git checkout -b Develop origin/main
git push -u origin Develop

# Delete Dev branch from remote
git push origin --delete Dev
```

### Alternative: Using GitHub Web Interface

1. **Create Develop branch:**
   - Go to repository on GitHub
   - Click on branch dropdown
   - Type "Develop" in the text field
   - Click "Create branch: Develop from 'main'"

2. **Delete Dev branch:**
   - Go to repository's branches page (Code → Branches)
   - Find "Dev" branch in the list
   - Click the trash icon next to it
   - Confirm deletion

### Verification:

After completing these steps, verify:
- ✓ Develop branch exists and points to the same commit as main
- ✓ Dev branch no longer exists in the repository
- ✓ All other branches remain unchanged


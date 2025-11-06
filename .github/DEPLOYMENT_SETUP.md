# GitHub Actions Deployment Setup

## Prerequisites
Your GitHub Actions workflow has been created at `.github/workflows/azure-functions-deploy.yml`

## Setup Instructions

### Step 1: Add GitHub Secret
You need to add the Azure Function App publish profile as a GitHub secret:

1. Go to your GitHub repository: https://github.com/DipikaBhandari/ssp
2. Click on **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Name: `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`
5. Value: Copy the entire contents of `publish-profile.xml` (in your project root)
6. Click **Add secret**

### Step 2: Get the Publish Profile
The publish profile has been saved to `publish-profile.xml` in your project root.

**Option 1 - Copy from file:**
```bash
cat publish-profile.xml
```

**Option 2 - Get it again from Azure:**
```bash
az functionapp deployment list-publishing-profiles \
  --resource-group cloud-database \
  --name hqfz4bou23pzsfa \
  --xml
```

### Step 3: Commit and Push
Once the secret is added, commit and push your changes:

```bash
git add .github/
git commit -m "Add GitHub Actions deployment workflow"
git push origin main
```

### Step 4: Verify Deployment
1. Go to your repository's **Actions** tab
2. You should see the workflow running
3. Click on the workflow run to see the deployment progress

## Workflow Details

- **Trigger**: Automatically runs on push to `main` branch
- **Manual trigger**: Can also be triggered manually from GitHub Actions tab
- **Build**: Uses .NET 8.0
- **Deploy**: Deploys to Azure Function App `hqfz4bou23pzsfa`

## Important Notes

⚠️ **Security**: 
- Delete `publish-profile.xml` after copying it to GitHub secrets
- Never commit the publish profile to your repository

```bash
rm publish-profile.xml
```

## Function App Details
- **Name**: hqfz4bou23pzsfa
- **URL**: https://hqfz4bou23pzsfa.azurewebsites.net
- **Resource Group**: cloud-database

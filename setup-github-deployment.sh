#!/bin/bash

# GitHub Actions Deployment Setup Script
# This script helps you set up the GitHub secret for Azure Functions deployment

echo "=============================================="
echo "GitHub Actions Deployment Setup"
echo "=============================================="
echo ""

# Check if publish-profile.xml exists
if [ ! -f "publish-profile.xml" ]; then
    echo "Error: publish-profile.xml not found!"
    echo "Run this command first:"
    echo "az functionapp deployment list-publishing-profiles --resource-group cloud-database --name hqfz4bou23pzsfa --xml > publish-profile.xml"
    exit 1
fi

echo "✓ Found publish-profile.xml"
echo ""

# Check if gh CLI is installed
if command -v gh &> /dev/null; then
    echo "GitHub CLI detected! Setting up secret automatically..."
    echo ""
    
    # Set the secret using gh CLI
    gh secret set AZURE_FUNCTIONAPP_PUBLISH_PROFILE < publish-profile.xml
    
    if [ $? -eq 0 ]; then
        echo "✓ Secret AZURE_FUNCTIONAPP_PUBLISH_PROFILE added successfully!"
        echo ""
        echo "Cleaning up..."
        rm publish-profile.xml
        echo "✓ Removed publish-profile.xml"
        echo ""
        echo "Next steps:"
        echo "1. Commit and push your changes:"
        echo "   git add .github/"
        echo "   git commit -m 'Add GitHub Actions deployment workflow'"
        echo "   git push origin main"
        echo ""
        echo "2. Check the Actions tab in your GitHub repository to see the deployment"
    else
        echo "✗ Failed to set secret. Please add it manually."
        echo ""
        echo "Follow the manual instructions in .github/DEPLOYMENT_SETUP.md"
    fi
else
    echo "GitHub CLI not found. Please add the secret manually."
    echo ""
    echo "Manual Setup Instructions:"
    echo "================================"
    echo ""
    echo "1. Copy the content of publish-profile.xml:"
    echo "   cat publish-profile.xml | pbcopy"
    echo ""
    echo "2. Go to: https://github.com/DipikaBhandari/ssp/settings/secrets/actions"
    echo ""
    echo "3. Click 'New repository secret'"
    echo ""
    echo "4. Name: AZURE_FUNCTIONAPP_PUBLISH_PROFILE"
    echo ""
    echo "5. Paste the copied content as the value"
    echo ""
    echo "6. Click 'Add secret'"
    echo ""
    echo "7. Delete the local file:"
    echo "   rm publish-profile.xml"
    echo ""
    echo "8. Commit and push:"
    echo "   git add .github/"
    echo "   git commit -m 'Add GitHub Actions deployment workflow'"
    echo "   git push origin main"
    echo ""
fi

echo "=============================================="

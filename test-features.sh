#!/bin/bash
#
# Quick feature test for Table Storage, SAS tokens, and API Authentication
#

set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:7071}"
API_KEY="${API_KEY:-dev-weather-app-key-12345}"

echo "========================================="
echo "Feature Testing"
echo "========================================="
echo ""

# Test 1: API Authentication - Should FAIL without key
echo "Test 1: API Auth - Request WITHOUT API key (should fail with 401)"
echo "--------------------------------------------------------------------"
RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" "$BASE_URL/api/test/image" 2>&1 || true)
HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
echo "Status: $HTTP_CODE"
if [ "$HTTP_CODE" == "401" ]; then
    echo "✓ PASS - Correctly rejected request without API key"
else
    echo "✗ FAIL - Expected 401, got $HTTP_CODE"
fi
echo ""

# Test 2: API Authentication - Should SUCCEED with key
echo "Test 2: API Auth - Request WITH API key (should succeed)"
echo "--------------------------------------------------------------------"
RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -H "X-API-Key: $API_KEY" "$BASE_URL/api/test/image" 2>&1 || true)
HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_CODE:/d')
echo "Status: $HTTP_CODE"
if [ "$HTTP_CODE" == "200" ]; then
    echo "✓ PASS - Request succeeded with API key"
    echo "$BODY" | python3 -m json.tool 2>/dev/null | head -20
else
    echo "✗ FAIL - Expected 200, got $HTTP_CODE"
    echo "$BODY"
fi
echo ""

# Test 3: Start a job and verify Table Storage
echo "Test 3: Start Job & Verify Table Storage"
echo "--------------------------------------------------------------------"
JOB_RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -H "X-API-Key: $API_KEY" -X POST "$BASE_URL/api/job/start" 2>&1 || true)
HTTP_CODE=$(echo "$JOB_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
BODY=$(echo "$JOB_RESPONSE" | sed '/HTTP_CODE:/d')

if [[ "$HTTP_CODE" == "202" || "$HTTP_CODE" == "200" ]]; then
    echo "✓ PASS - Job started successfully"
    JOB_ID=$(echo "$BODY" | jq -r '.JobId // .jobId // empty')
    echo "Job ID: $JOB_ID"
    echo ""
    
    # Wait a bit for processing
    echo "Waiting 5 seconds for processing..."
    sleep 5
    
    # Check status from Table Storage
    echo ""
    echo "Fetching status from Table Storage..."
    STATUS_RESPONSE=$(curl -s -H "X-API-Key: $API_KEY" "$BASE_URL/api/job/$JOB_ID" 2>&1 || true)
    echo "$STATUS_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$STATUS_RESPONSE"
    
    # Check if we have processed stations count (indicates Table Storage working)
    PROCESSED=$(echo "$STATUS_RESPONSE" | jq -r '.ProcessedStations // 0' 2>/dev/null)
    TOTAL=$(echo "$STATUS_RESPONSE" | jq -r '.TotalStations // 0' 2>/dev/null)
    
    echo ""
    echo "Progress: $PROCESSED/$TOTAL stations processed"
    
    if [ "$PROCESSED" -gt "0" ]; then
        echo "✓ PASS - Table Storage is tracking progress!"
    else
        echo "⚠ INFO - Processing may still be in progress"
    fi
    
    # Test 4: Check for SAS token in image URLs
    echo ""
    echo "Test 4: SAS Token in Image URLs"
    echo "--------------------------------------------------------------------"
    
    # Wait for at least one image
    for i in {1..10}; do
        STATUS_RESPONSE=$(curl -s -H "X-API-Key: $API_KEY" "$BASE_URL/api/job/$JOB_ID" 2>&1 || true)
        IMAGES=$(echo "$STATUS_RESPONSE" | jq '.Images // []' 2>/dev/null)
        if [ "$IMAGES" != "[]" ] && [ "$IMAGES" != "null" ]; then
            break
        fi
        echo "Waiting for images... ($i/10)"
        sleep 2
    done
    
    # Extract first image URL
    IMAGE_URL=$(echo "$STATUS_RESPONSE" | jq -r '.Images[0].ImageUrl // empty' 2>/dev/null)
    
    if [ -n "$IMAGE_URL" ]; then
        echo "Sample Image URL:"
        echo "$IMAGE_URL" | head -c 150
        echo "..."
        echo ""
        
        # Check for SAS token parameters
        if [[ "$IMAGE_URL" =~ "sig=" ]] && [[ "$IMAGE_URL" =~ "se=" ]] && [[ "$IMAGE_URL" =~ "sp=" ]]; then
            echo "✓ PASS - SAS token detected in URL (contains sig=, se=, sp= parameters)"
        else
            echo "✗ FAIL - No SAS token found in URL"
        fi
    else
        echo "⚠ INFO - No images generated yet"
    fi
    
else
    echo "✗ FAIL - Job failed to start: $HTTP_CODE"
    echo "$BODY"
fi

echo ""
echo "========================================="
echo "Testing Complete"
echo "========================================="

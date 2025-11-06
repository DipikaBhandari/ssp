# Weather Image Generator

A serverless Azure Functions app that generates weather-themed images for 50 Dutch weather stations. Built with .NET 8, it fetches real-time weather data from Buienradar API, overlays it on landscape images, and stores results in Azure Blob Storage.

## What It Does

1. **Start a job** via HTTP endpoint → Returns job ID instantly
2. **Queue processes** weather data for 50 stations in parallel
3. **Image generation**: Each station gets a landscape photo with weather overlay
4. **Check status** via HTTP endpoint → Returns progress and image URLs

## Quick Start

### Prerequisites
- .NET 8 SDK
- Azure subscription
- (Optional) Unsplash API key for custom images

### Local Development

```bash
# 1. Install Azurite (storage emulator)
npm install -g azurite

# 2. Start Azurite
azurite --silent --location ./azurite

# 3. Restore dependencies
dotnet restore

# 4. Run the app
dotnet run
```

The app will be available at `http://localhost:7071`

### Test It

```bash
# Start a job
curl -X POST http://localhost:7071/api/job/start

# Check status (use the jobId from above)
curl http://localhost:7071/api/job/status/{jobId}
```

## Deploy to Azure

### Option 1: Automated (PowerShell)

```powershell
./deploy.ps1
```

### Option 2: Manual

```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-weather-image --location westeurope

# Create storage account
az storage account create --name stweatherimg --resource-group rg-weather-image --location westeurope

# Create function app
az functionapp create --name func-weather-image --resource-group rg-weather-image --storage-account stweatherimg --consumption-plan-location westeurope --runtime dotnet-isolated --runtime-version 8 --functions-version 4

# Deploy code
func azure functionapp publish func-weather-image
```

## API Endpoints

### POST /api/job/start
Starts a new image generation job.

**Response:**
```json
{
  "jobId": "abc123",
  "message": "Job started",
  "statusUrl": "http://localhost:7071/api/job/status/abc123"
}
```

### GET /api/job/status/{jobId}
Gets job status and generated image URLs.

**Response:**
```json
{
  "jobId": "abc123",
  "status": "InProgress",
  "totalImages": 50,
  "completedImages": 25,
  "imageUrls": ["https://..."]
}
```

## Architecture

```
HTTP Request → StartJobFunction
                    ↓
            [job-request-queue]
                    ↓
        StartJobQueueFunction (fetches 50 weather stations)
                    ↓
       [image-processing-queue] (50 messages)
                    ↓
        ProcessImageFunction (parallel processing)
                    ↓
            Blob Storage (images)
            Table Storage (status)
```

### Key Features
- **Two-queue system**: Separates job initiation from processing
- **Parallel processing**: All 50 images generated simultaneously
- **Fast response**: API returns immediately while processing in background
- **Persistent storage**: Images in Blob Storage, status in Table Storage

## Configuration

Edit `local.settings.json`:

```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "StorageConnectionString": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "UnsplashAccessKey": "YOUR_KEY_HERE",
    "BlobContainerName": "weather-images"
  }
}
```

## Project Structure

```
Functions/
  ├── StartJobFunction.cs          # HTTP trigger - starts jobs
  ├── StartJobQueueFunction.cs     # Queue trigger - fetches weather data
  ├── ProcessImageFunction.cs      # Queue trigger - generates images
  └── GetJobStatusFunction.cs      # HTTP trigger - returns status

Models/
  ├── WeatherStation.cs            # Weather data model
  ├── JobRequest.cs                # Job request/response
  └── JobStatus.cs                 # Status tracking

Services/
  ├── WeatherService.cs            # Buienradar API client
  ├── ImageService.cs              # Image processing (ImageSharp)
  ├── BlobStorageService.cs        # Blob operations
  └── TableStorageService.cs       # Status tracking
```

## Monitoring

In Azure Portal:
1. Go to your Function App
2. Click **Application Insights**
3. View logs, performance, and failures

Locally with Azure CLI:
```bash
func azure functionapp logstream func-weather-image
```

## Troubleshooting

**Azurite not starting?**
```bash
azurite --silent --location ./azurite --loose --skipApiVersionCheck
```

**Function not triggering?**
- Check queue exists: `./setup-local.sh`
- Verify connection string in `local.settings.json`
- Check Azurite is running on ports 10000-10002

**Images not generating?**
- Unsplash key is optional (uses fallback)
- Check Application Insights for errors
- Verify storage permissions

## Tech Stack

- **.NET 8** - Isolated worker model
- **Azure Functions v4** - Serverless compute
- **Azure Storage** - Queues, Blobs, Tables
- **ImageSharp** - Image processing
- **Buienradar API** - Weather data
- **Unsplash API** - Landscape images (optional)

## License

This project is for educational purposes.
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "StorageConnectionString": "UseDevelopmentStorage=true",
    "UnsplashAccessKey": "YOUR_UNSPLASH_ACCESS_KEY",
    "ApiKey": "dev-weather-app-key-12345",
    "BlobContainerName": "weather-images"
  }
}
```

### 5. Run Locally

```bash
func start
# or
dotnet run
```

### 6. Test the Two-Queue Flow

**Option 1: Automated test**
```bash
./test-two-queue.sh
```

**Option 2: Manual testing**
```bash
# Start a new job (with API key)
curl -X POST http://localhost:7071/api/job/start \
  -H "X-API-Key: dev-weather-app-key-12345"

# Check job status (replace {jobId} with the ID returned from previous call)
curl http://localhost:7071/api/job/{jobId} \
  -H "X-API-Key: dev-weather-app-key-12345"
```

Watch the logs to see the two-queue flow:
1. Message enqueued to `job-request-queue`
2. `StartJobQueueFunction` processes it
3. 36 messages enqueued to `image-processing-queue`
4. `ProcessImageFunction` processes them in parallel

## Deployment to Azure

### Quick Deploy

```powershell
# Deploy with all resources
./deploy.ps1 -ResourceGroupName "weatherimage-rg" -Location "westeurope"

# Deploy with Unsplash API key
./deploy.ps1 -ResourceGroupName "weatherimage-rg" -Location "westeurope" -UnsplashAccessKey "YOUR_KEY"
```

### Manual Deployment Steps

1. **Login to Azure**
```bash
az login
```

2. **Create Resource Group**
```bash
az group create --name weatherimage-rg --location westeurope
```

3. **Deploy Infrastructure**
```bash
az deployment group create \
  --resource-group weatherimage-rg \
  --template-file ./deploy/main.bicep
```

4. **Build and Publish**
```bash
dotnet publish --configuration Release --output ./publish
```

5. **Create Deployment Package**
```bash
cd publish
zip -r ../function.zip .
cd ..
```

6. **Deploy Function App**
```bash
az functionapp deployment source config-zip \
  --resource-group weatherimage-rg \
  --name <function-app-name> \
  --src function.zip
```

## API Documentation

### Start Job

**Endpoint**: `POST /api/job/start`

**Description**: Starts a new weather image generation job.

**Response**:
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "createdAt": "2025-10-31T12:00:00Z"
}
```

**Status Codes**:
- `202 Accepted`: Job started successfully
- `503 Service Unavailable`: Unable to fetch weather data
- `500 Internal Server Error`: Server error

### Get Job Status

**Endpoint**: `GET /api/job/{jobId}`

**Description**: Retrieves the status and results of a job.

**Response** (In Progress):
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "processing",
  "createdAt": "2025-10-31T12:00:00Z",
  "completedAt": null,
  "totalStations": 50,
  "processedStations": 23,
  "imageUrls": [],
  "errorMessage": null
}
```

**Response** (Completed):
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "completed",
  "createdAt": "2025-10-31T12:00:00Z",
  "completedAt": "2025-10-31T12:05:32Z",
  "totalStations": 50,
  "processedStations": 50,
  "imageUrls": [
    "https://<storage>.blob.core.windows.net/weather-images/550e.../station-000.jpg",
    "https://<storage>.blob.core.windows.net/weather-images/550e.../station-001.jpg",
    "..."
  ],
  "errorMessage": null
}
```

**Status Codes**:
- `200 OK`: Job found
- `404 Not Found`: Job ID not found
- `500 Internal Server Error`: Server error

## Project Structure

```
├── Functions/
│   ├── StartJobFunction.cs          # HTTP trigger to start jobs
│   ├── ProcessImageFunction.cs      # Queue trigger for image processing
│   └── GetJobStatusFunction.cs      # HTTP trigger to get job status
├── Models/
│   ├── WeatherStation.cs            # Weather station data model
│   ├── JobRequest.cs                # Job request model
│   ├── JobStatus.cs                 # Job status model
│   ├── ImageProcessingMessage.cs    # Queue message model
│   └── BuienradarResponse.cs        # API response models
├── Services/
│   ├── IWeatherService.cs           # Weather service interface
│   ├── WeatherService.cs            # Buienradar API integration
│   ├── IImageService.cs             # Image service interface
│   ├── ImageService.cs              # Image processing with ImageSharp
│   ├── IBlobStorageService.cs       # Blob storage interface
│   ├── BlobStorageService.cs        # Azure Blob Storage operations
│   ├── IQueueService.cs             # Queue service interface
│   └── QueueService.cs              # Azure Queue operations
├── deploy/
│   └── main.bicep                   # Infrastructure as Code
├── deploy.ps1                       # Deployment script
├── Program.cs                       # Application entry point
├── host.json                        # Function host configuration
├── local.settings.json              # Local settings (not in git)
├── WeatherImageApp.csproj           # Project file
└── README.md                        # This file
```

## Configuration

### Application Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `AzureWebJobsStorage` | Storage connection for Functions runtime | Connection string |
| `StorageConnectionString` | Storage connection for app | Connection string |
| `BlobContainerName` | Container name for images | `weather-images` |
| `UnsplashAccessKey` | Unsplash API key (optional) | Your API key |

## Queue Configuration

- **image-processing-queue**: Messages for individual station image processing
- **status-update-queue**: Messages for status updates (future use)

## Storage Configuration

- **Container**: `weather-images` (public blob access)
- **Naming**: `{jobId}/station-{index}.jpg`

## Monitoring

View logs and metrics in:
- **Azure Portal**: Application Insights
- **Local Development**: Console output

## Troubleshooting

### Local Development Issues

**Azurite not starting**:
```bash
# Kill existing Azurite processes
pkill -f azurite

# Start fresh
azurite --silent --location ./azurite
```

**Queue messages not processing**:
- Ensure Azurite is running
- Check queue exists: Storage Explorer or Azure Storage Explorer
- Verify connection string in `local.settings.json`

### Deployment Issues

**Authentication errors**:
```bash
az login
az account set --subscription "Your-Subscription-Name"
```

**Function app not responding**:
- Wait 2-3 minutes after deployment
- Check Application Insights for errors
- Restart function app: `az functionapp restart`

## Performance

- **Cold Start**: ~5-10 seconds (Consumption plan)
- **Processing Time**: ~50-100 seconds for 50 images (parallel processing)
- **Throughput**: Limited by Unsplash API rate limits

## Security Considerations

- Blob container has public access for image URLs
- HTTP endpoints are anonymous (add authentication in production)
- Store sensitive keys in Azure Key Vault (production)

## Future Enhancements

- [ ] SAS tokens for secure blob access
- [ ] Authentication on API endpoints
- [ ] Progress tracking with Azure Table Storage
- [ ] GitHub Actions for CI/CD
- [ ] Custom domain and CDN for images
- [ ] Image caching mechanism
- [ ] Webhook notifications on completion

## License

This project is for educational purposes as part of an Azure/GitHub assignment.

## Author

Created for InHolland Assignment - triplegh2025

## Support

For issues or questions, please contact triplegithub2025@outlook.com

# Week 3 Implementation Documentation - Image Generation

## Overview
Week 3 deliverables focused on implementing the AI image generation core functionality, including DALL-E API integration, image storage, generation history tracking, and API endpoints.

## Completed Components

### 1. DALL-E Integration
- Added OpenAI SDK (v2.1.0) package to the HomeDecorator.Api project
- Implemented `DalleGenerationService.cs` that uses the OpenAI client to generate images
- Implemented enhanced prompting system for home decoration context

### 2. Image Storage Solutions
- Created `IStorageService` interface with methods for storing and managing images
- Implemented `S3StorageService` for cloud storage (production usage)
- Implemented `LocalStorageService` for development/testing (no cloud dependency)
- Created `MockStorageService` for test environments

### 3. Image Generation Data Models
- Created `ImageRequest.cs` model for tracking generation requests
- Implemented `CreateImageRequestDto` for API input
- Implemented `ImageRequestResponseDto` for API response

### 4. Database Operations
- Created `IImageRequestRepository` interface for database operations
- Implemented `SqliteImageRequestRepository.cs` with CRUD operations for image requests
- Set up proper indexing and query optimizations

### 5. Orchestration Service
- Created `ImageGenerationOrchestrator.cs` to coordinate the full workflow:
  - Credit checking
  - Image generation
  - Image storage
  - Database persistence
  - Product matching (foundation for Week 4)

### 6. API Endpoints
- Created `ImageGenerationEndpoints.cs` with RESTful endpoints:
  - POST /api/image-request - Create a new image generation request
  - GET /api/image-request/{id} - Get details of a specific request
  - GET /api/history - Get user's generation history

### 7. MAUI App Integration
- Updated `ApiService.cs` in the MAUI app with methods to call the image generation endpoints
- Updated UI pages to use the API service when not in fake data mode

## Running the Application

### Prerequisites
- .NET 9.0 SDK
- OpenAI API key (for DALL-E integration)

### Configuration
1. Set up user secrets for the API key:
   ```powershell
   cd src/HomeDecorator.Api
   dotnet user-secrets set "DallE:ApiKey" "your-openai-api-key"
   ```

2. For local development/testing:
   ```powershell
   dotnet user-secrets set "Storage:LocalPath" "wwwroot/images"
   ```

3. Set `IsFakeDataMode` to `false` in appsettings.json to use real APIs.

### Testing Workflow
1. Start the API project: `dotnet run --project src/HomeDecorator.Api`
2. Start the MAUI app: `dotnet run --project src/HomeDecorator.MauiApp`
3. In the MAUI app:
   - Navigate to the "New Design" page
   - Enter a prompt for image generation
   - Click "Generate Design"
   - The app will show status updates and the final image when complete
   - The image generation history will be visible on the "History" page

## Authentication Notes
Current implementation uses a test user ("test-user") for all API calls.
Authentication will be enhanced in future iterations.

## Future Enhancements
- Implement proper JWT authentication
- Add image optimization for faster delivery
- Add retry mechanisms for API calls
- Implement real-time status updates using SignalR

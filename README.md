# Clean Architecture .NET 8 API Template

Enterprise-grade .NET 8 API template with Clean Architecture, CQRS pattern, JWT authentication, and Docker deployment.

## 🏗️ Architecture

- **Clean Architecture** - Separation of concerns with Domain, Application, Infrastructure, and API layers
- **CQRS Pattern** - Command Query Responsibility Segregation with MediatR
- **JWT Authentication** - Secure token-based authentication
- **Entity Framework Core** - Modern ORM with SQL Server
- **FluentValidation** - Robust input validation
- **AutoMapper** - Object mapping
- **Docker** - Containerized deployment

## 🚀 Features

### ✅ Core Features
- Clean Architecture implementation
- CQRS with MediatR
- JWT authentication & authorization
- Entity Framework Core with SQL Server
- FluentValidation for input validation
- AutoMapper for object mapping
- Global exception handling
- Swagger/OpenAPI documentation
- Docker containerization

### ✅ Security
- BCrypt password hashing
- JWT token generation
- Authorization attributes
- Input validation
- SQL injection protection

### ✅ Development
- Hot reload support
- Swagger UI for API testing
- Comprehensive error handling
- Logging configuration
- Development/Production environments

## 📁 Project Structure

```
App/
├── App.Api/                 # API Layer (Controllers, Middleware)
├── App.Application/         # Application Layer (Commands, Queries, Handlers)
├── App.Domain/             # Domain Layer (Entities, Interfaces)
├── App.Infrastructure/     # Infrastructure Layer (Database, External Services)
├── Dockerfile              # Docker configuration
├── docker-compose.yml      # Docker Compose setup
└── README.md              # This file
```

## 🛠️ Setup & Installation

### Prerequisites
- .NET 8 SDK
- SQL Server (or Docker)
- Docker (optional, for containerized deployment)

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd App
   ```

2. **Update connection string**
   ```json
   // appsettings.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=AppDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true"
     }
   }
   ```

3. **Run database migrations**
   ```bash
   cd App.Api
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the API**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger

### Docker Deployment

1. **Build and run with Docker Compose**
   ```bash
   docker-compose up --build
   ```

2. **Access the application**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger

## 🔐 Authentication

### JWT Configuration
```json
{
  "Jwt": {
    "Key": "your-super-secret-key-here-minimum-16-characters-long",
    "Issuer": "your-app",
    "Audience": "your-app-users"
  }
}
```

### API Endpoints

#### Public Endpoints
- `POST /api/users` - Register new user
- `POST /api/users/login` - Login user

#### Protected Endpoints (Require JWT Token)
- `GET /api/users/{id}` - Get user by ID
- `GET /api/users/by-email?email=...` - Get user by email
- `GET /api/users/by-username?username=...` - Get user by username
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Using JWT Token
```bash
# Login to get token
curl -X POST "http://localhost:5000/api/users/login" \
  -H "Content-Type: application/json" \
  -d '{"emailOrUsername": "user@example.com", "password": "password123"}'

# Use token in subsequent requests
curl -X GET "http://localhost:5000/api/users/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 🧪 Testing

### Swagger UI
1. Navigate to http://localhost:5000/swagger
2. Use the interactive API documentation
3. Test endpoints directly from the browser

### Postman/Insomnia
1. Import the API endpoints
2. Set up authentication with JWT tokens
3. Test all CRUD operations

## 🔧 Configuration

### Environment Variables
```bash
# Development
ASPNETCORE_ENVIRONMENT=Development

# Production
ASPNETCORE_ENVIRONMENT=Production
```

### Database Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AppDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true"
  }
}
```

## 📦 Dependencies

### Core Packages
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT authentication
- `MediatR` - CQRS pattern implementation
- `AutoMapper` - Object mapping
- `FluentValidation` - Input validation
- `Entity Framework Core` - Database ORM
- `BCrypt.Net-Next` - Password hashing

## 🚀 Deployment

### Docker
```bash
# Build and run
docker-compose up --build

# Run in background
docker-compose up -d

# Stop services
docker-compose down
```

### Azure/AWS
1. Build the application
2. Deploy to your cloud provider
3. Configure environment variables
4. Set up database connection

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License.

## 🆘 Support

For support and questions:
- Create an issue in the repository
- Check the documentation
- Review the code examples

---

**Built with ❤️ using Clean Architecture and .NET 8** 
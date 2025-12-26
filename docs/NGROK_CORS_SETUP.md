# Ngrok CORS Setup for Backend

## The Problem

You're seeing only OPTIONS requests in ngrok logs because:

1. Browsers send preflight OPTIONS requests for CORS
2. Your backend isn't configured to handle these OPTIONS requests properly
3. The actual GET request never happens because the preflight fails

## The Solution

### For .NET Backend (C#)

Add this to your `Program.cs` or `Startup.cs`:

```csharp
// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("*")
            // Important: Allow the ngrok header
            .WithHeaders("ngrok-skip-browser-warning", "Authorization", "Content-Type");
    });
});

// After building the app, use CORS middleware
var app = builder.Build();

app.UseCors("AllowAll");

// Rest of your middleware...
app.UseAuthentication();
app.UseAuthorization();
```

### For Node.js/Express Backend

Install cors package:

```bash
npm install cors
```

Add to your server file:

```javascript
const express = require("express");
const cors = require("cors");

const app = express();

// Configure CORS
app.use(
  cors({
    origin: "*", // Or specify your frontend URL
    methods: ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"],
    allowedHeaders: [
      "Content-Type",
      "Authorization",
      "ngrok-skip-browser-warning",
    ],
    credentials: true,
  })
);

// Handle preflight requests
app.options("*", cors());

// Your routes...
```

### For Python/Flask Backend

```python
from flask import Flask
from flask_cors import CORS

app = Flask(__name__)

# Configure CORS
CORS(app, resources={
    r"/api/*": {
        "origins": "*",
        "methods": ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"],
        "allow_headers": [
            "Content-Type",
            "Authorization",
            "ngrok-skip-browser-warning"
        ]
    }
})
```

## Testing the Fix

After configuring CORS on your backend:

1. Check ngrok logs at `http://127.0.0.1:4040`
2. You should see:
   - OPTIONS /api/birds (preflight)
   - GET /api/birds (actual request)
3. Both should return 200 status

## Important Notes

- The `ngrok-skip-browser-warning` header is only needed when accessing ngrok URLs directly in a browser
- For React Native apps, this header helps but isn't strictly necessary
- The real issue is CORS configuration on your backend
- Make sure OPTIONS requests return 200 status with proper CORS headers

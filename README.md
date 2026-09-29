# SecureShare 🔐

SecureShare is a secure file-sharing web application built using **ASP.NET Core MVC**.  
The project focuses on securely uploading, storing, and managing user files with authentication, authorization, and encryption at rest.

The main goal of this project is to understand how secure file storage and file-sharing systems can be designed using ASP.NET Core and .NET security features.

---

## 🚀 Features

### Authentication
- User registration and login
- ASP.NET Core Identity
- Secure password hashing
- Cookie-based authentication
- Authentication-protected file operations

### Secure File Upload
- Upload documents and images
- Maximum file size: **10 MB**
- Allowed file types:
  - PDF
  - JPG
  - JPEG
  - PNG
  - ZIP
  - DOCX
- Empty file validation
- File extension validation
- Randomly generated stored filenames using GUIDs

### Encrypted File Storage
- AES-256 encryption for uploaded files
- Encrypted files are stored outside `wwwroot`
- Encryption key is stored using ASP.NET Core User Secrets during development
- A unique IV is generated for each encrypted file
- Original files cannot be directly opened from the `Uploads` directory

### File Management
- View files uploaded by the currently logged-in user
- Store file metadata in SQL Server
- Download files using their original filenames

---

## 🛠️ Technologies Used

| Technology | Purpose |
|------------|---------|
| C# | Programming language |
| ASP.NET Core MVC | Web application framework |
| .NET 10 | Application runtime |
| ASP.NET Core Identity | Authentication and user management |
| Entity Framework Core | Database access |
| SQL Server / LocalDB | Database |
| AES-256 | File encryption |
| Bootstrap | UI styling |
| Razor Views | Frontend |

---

## 📁 Project Structure

```text
SecureShare/
│
├── Controllers/
│   └── FileController.cs
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── Models/
│   ├── File.cs
│   └── FileUploadViewModel.cs
│
├── Services/
│   └── EncryptionService.cs
│
├── Views/
│   ├── File/
│   │   ├── Upload.cshtml
│   │   └── MyFiles.cshtml
│   │
│   └── ...
│
├── Uploads/
│   └── Encrypted uploaded files
│
├── wwwroot/
│   └── CSS, JavaScript, Bootstrap, etc.
│
├── appsettings.json
├── Program.cs
└── README.md
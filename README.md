# SecureShare 🔐

SecureShare is a secure file-sharing web application built using **ASP.NET Core MVC**.

The project focuses on securely uploading, storing, managing, and sharing files with authentication, authorization, and encryption at rest.

The main goal of this project is to understand how secure file storage and file-sharing systems can be designed using **ASP.NET Core and .NET security features**.

---

## 🚀 Features

### Authentication

- User registration and login
- ASP.NET Core Identity
- Secure password hashing
- Cookie-based authentication
- Authentication-protected file operations
- User-specific file access

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
- Files are decrypted only when an authorized user downloads them

### File Management

- View files uploaded by the currently logged-in user
- Store file metadata in SQL Server
- Download files using their original filenames
- Delete uploaded files
- File size and upload date information
- File type information

### Secure Share Links

- Generate unique share links for files
- Optional password protection
- Optional link expiration
- Optional maximum download limit
- Track download count
- Revoke share links
- Manage created share links
- Copy generated share links directly from the interface
- Invalid, expired, revoked, and exceeded-limit links are handled with user-friendly messages

### Direct User Sharing

- Share files directly with registered SecureShare users
- Share files using the recipient's email address
- Prevent sharing files with yourself
- Prevent duplicate active permissions
- Restore previously revoked permissions
- Store file permissions in the database
- Default permission level: **Viewer**

### Shared With Me

- View files shared directly with the logged-in user
- Display file name, size, sharing date, and permission
- Download files shared with the user
- Download authorization checks active file permissions
- Revoked permissions cannot be used to download files

### User Interface

- Responsive Bootstrap-based interface
- SecureShare branded navigation
- My Files section
- Shared With Me section
- Upload section
- Manage Shares section
- Clean file management tables
- File type indicators
- User-friendly success and error messages

---

## 🛠️ Technologies Used

| **Technology** | **Purpose** |
|---|---|
| C# | Programming language |
| ASP.NET Core MVC | Web application framework |
| .NET 10 | Application runtime |
| ASP.NET Core Identity | Authentication and user management |
| Entity Framework Core | Database access and ORM |
| SQL Server / LocalDB | Database |
| AES-256 | File encryption |
| Bootstrap | UI styling |
| Razor Views | Frontend |
| User Secrets | Development secret management |

---

## 🔐 Security

SecureShare currently implements several security mechanisms:

- ASP.NET Core Identity for authentication
- Secure password hashing through ASP.NET Core Identity
- `[Authorize]` protection for authenticated file operations
- User-specific file ownership
- Permission-based access for directly shared files
- AES-256 encryption for files stored on disk
- Randomly generated stored filenames
- Files stored outside the publicly accessible `wwwroot` directory
- Random cryptographically generated share tokens
- Optional password-protected share links
- Share link expiration
- Share link download limits
- Share link revocation

> **Note:** The current encryption implementation is intended for learning and project purposes. Additional production-level security hardening such as authenticated encryption, secure production key management, malware scanning, and improved streaming can be added in future development.

---

## 📁 Project Structure

```text
SecureShare/
│
├── Controllers/
│   ├── FileController.cs
│   ├── HomeController.cs
│   └── ShareController.cs
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── Models/
│   ├── File.cs
│   ├── FilePermission.cs
│   ├── FileShare.cs
│   ├── FileUploadViewModel.cs
│   └── SharePasswordViewModel.cs
│
├── Services/
│   └── EncryptionService.cs
│
├── Views/
│   ├── File/
│   │   ├── Upload.cshtml
│   │   ├── MyFiles.cshtml
│   │   └── SharedWithMe.cshtml
│   │
│   ├── Share/
│   │   ├── Manage.cshtml
│   │   ├── Password.cshtml
│   │   └── Error.cshtml
│   │
│   └── ...
│
├── Uploads/
│   └── Encrypted uploaded files
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── lib/
│
├── appsettings.json
├── Program.cs
└── README.md

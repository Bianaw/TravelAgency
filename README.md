Project Overview
TravelAgencyService is a full-stack ASP.NET Core MVC web application for managing and booking travel packages.
The system supports user registration, trip browsing, booking management, payments simulation, waiting lists, email notifications, and administrative control.
Main Features:
User authentication and role management (Admin / User)
Travel packages listing with search and filters
Trip booking, shopping cart, and payment flow
Waiting list for fully booked trips
Booking cancellation based on business rules
Email notifications (payment confirmation, trip reminders)

Technologies Used:
ASP.NET Core MVC
Entity Framework Core
SQL Server
ASP.NET Identity
SMTP (Email Notifications)
Bootstrap 5
C#
HTML / CSS / JavaScript

Setup Instructions:
     1-Clone the repository
     2-Open the project in Visual Studio
     3-Configure the database connection in appsettings.json
     4- database:
         4.1-Option A:Run database migrations: Update-Database  
         4.2-Option B – SQL Script: 
                              1- Open SQL Server Management Studio (or SQL Server Object Explorer).
                              2-Create a new empty database.
                              3-Run the SQL script located in the repository (script.txt) to create all require tables.
     5-Run the project

# StayHub

## About the Project

Many property owners have apartments that stay empty for months without generating any income, while travelers often struggle to find trusted places to stay. StayHub aims to solve this problem by making it easy for property owners to rent out their apartments and for guests to discover, book, and manage their stays in one place.
### What you can do

#### For Property Owners

* Publish and manage apartment listings
* Upload and manage apartment images
* Add amenities and property details
* Block unavailable dates
* Manage bookings and reservations

#### For Guests

* Browse available apartments
* Search and filter listings
* Book apartments online
* Save favorite properties
* Leave reviews after completed stays
* Communicate directly with property owners

#### Platform Features

* Booking lifecycle management
* Payment integration
* Notifications
* Role-based access control
* Secure authentication
* Background job processing

---

## Architecture

StayHub is designed as a **production-inspired backend** that focuses on writing clean, maintainable, and scalable software.

The project follows:

* Clean Architecture
* Domain-Driven Design (DDD)
* CQRS
* Domain Events
* Repository & Unit of Work Patterns

---

## Tech Stack

### Backend

* ASP.NET Core (.NET 10)
* C#
* Entity Framework Core
* Dapper
* MediatR
* FluentValidation

### Database & Caching

* PostgreSQL
* Redis

### Authentication & Authorization

* Keycloak
* OAuth2
* OpenID Connect

### Infrastructure

* Amazon S3 for image storage
* Hangfire for background jobs
* Docker & Docker Compose
* Azure DevOps CI/CD Pipeline
* Serilog for structured logging

---

## Project Status

> 🚧 **StayHub is currently under active development.**

The project is being built step by step, with each phase introducing new business features and production-ready engineering practices. The goal is not only to build a apartment rental platform, but also to demonstrate how modern .NET applications are designed, developed, tested, and deployed.

-- db/init.sql
-- Creates database, app user, and tables for DoleVisitorLogbook (MySQL).
-- Database: dole_library_logbook
-- IMPORTANT: Replace 'ChangeThisStrongPassword!' before running in any production environment.

-- Create database
CREATE DATABASE IF NOT EXISTS dole_library_logbook
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_general_ci;

-- Create application user (change host and password as required)
CREATE USER IF NOT EXISTS 'dole_app'@'localhost' IDENTIFIED BY 'ChangeThisStrongPassword!';

-- Grant least-privilege permissions needed by the app
GRANT SELECT, INSERT, UPDATE, DELETE ON dole_library_logbook.* TO 'dole_app'@'localhost';
FLUSH PRIVILEGES;

-- Use the database
USE dole_library_logbook;

-- Create users table
CREATE TABLE IF NOT EXISTS users (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    role VARCHAR(20) NOT NULL,
    created_at DATETIME,
    updated_at DATETIME
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Create visitors table
CREATE TABLE IF NOT EXISTS visitors (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    gender VARCHAR(50),
    client_type VARCHAR(50),
    office VARCHAR(255),
    purpose TEXT,
    time_in DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    time_out DATETIME
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Insert sample admin user (password: admin123 hashed with SHA256)
-- Note: Change this password immediately in production
INSERT INTO users (username, password, full_name, role, created_at, updated_at)
VALUES ('admin', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f1979c67f', 'Administrator', 'Admin', NOW(), NOW())
ON DUPLICATE KEY UPDATE username=username;

-- Create indexes for common queries
CREATE INDEX idx_time_in ON visitors(time_in);
CREATE INDEX idx_name ON visitors(name);
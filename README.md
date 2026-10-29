# Test Case Management System (TCM)

> **Master's Thesis Project**
>
> *Design and Implementation of a Test Case Management System Supporting Software Quality Assurance Processes*
>
> **Author:** Konrad Siuta
>
> **Institution:** University of the National Education Commission in Krakow (UKEN) — Institute of Security and Computer Science

## Project Overview

The **TCM System** is an enterprise-grade, distributed Test Case Management application engineered to bridge the gap between software specifications, manual testing activities, and automated execution results.

Unlike traditional flat CRUD applications, this system introduces an **algorithmically enforced Traceability Engine** capable of detecting version drifts (*Outdated Coverage*) between evolving project requirements and existing test suites, ensuring absolute test suite integrity across software development lifecycles.

## AI Assistance & Attribution Disclosure

* **Architecture & Design:** The domain modeling, system architecture, relational database design, Traceability algorithms, UI/UX concepts, and security policies were conceived, designed, and specified by the author (Konrad Siuta).
* **Code Implementation:** Generative AI tools were utilized during the development phase as an assistant for boilerplate generation, refactoring, and code implementation based on the author's strict architectural directives and domain specifications.

## Architectural Overview & Tech Stack

The solution follows a multi-tiered, decoupled **Client-Server Architecture**:

```
[ Desktop Client (WPF) ] <--- HTTP / REST (JWT) ---> [ ASP.NET Core Web API ] <--- Entity Framework Core ---> [ MySQL Database ]
         │                                                      │
     MVVM Pattern                                        Reverse Proxy (Nginx)
  XAML DataTriggers                                       Systemd Service (Linux)
```

* **Backend Framework:** ASP.NET Core Web API (.NET 9)
* **Frontend Application:** Windows Presentation Foundation (WPF) with MVVM Architecture
* **Database & ORM:** MySQL using Entity Framework Core (*Code-First* approach)
* **Authentication & Security:** JSON Web Tokens (JWT) & Role-Based Access Control (RBAC)
* **Deployment Stack:** Linux (Debian 12), nginx (reverse proxy), systemd

## Key System Features

### 1. Version-Aware Traceability Engine (Core Innovation)

* Establishes dynamic many-to-many relationships between business requirements and test cases.
* Captures historical snapshots at the moment a test case is assigned.
* Automatically flags **Outdated Coverage** when requirements evolve to a higher version.
* Highlights version conflicts dynamically in the UI using native WPF `DataTriggers` without code-behind overhead.

### 2. Defense-in-Depth Security (RBAC)

* Stateless authentication using signed JWT Tokens.
* Granular role assignment (`Administrator`, `Tester`, `Viewer`) scoped per project.
* Dual-layer access enforcement: server-side API controller validation and client-side UI binding adjustments.

### 3. Execution Management & Hybrid Automation Import

* Supports manual test cycle executions with defect linking (e.g., Jira URL integration) and log attachments.
* Features a built-in XML Parser engine (`System.Xml.Linq`) capable of streaming and mapping automated test run results directly to database entities.
* Exports formal PDF test run summary reports.

### 4. Data Ingestion & History Preservation

* Streamed CSV Import for bulk requirement loading with duplicate prevention.
* Configurable regular expression (RegEx) parsers for ingesting test cases directly from source code docstrings (e.g., Doxygen/Python comments).
* Implements a **Soft Delete** strategy for test cases to preserve historical execution integrity.

## Deployment Specifications

The production server is deployed in a Linux environment with the following infrastructure configuration:

* **Publish Mode:** Self-Contained (`linux-x64`), bundling the runtime with executable binaries.
* **Service Management:** Managed by `systemd` for auto-restart on failure and system startup execution.
* **Web Server:** Nginx configured as a Reverse Proxy binding port `80` to the internal Kestrel port (`5000`).

## Legal & Copyright Notice

### **ALL RIGHTS RESERVED**

```
Copyright (c) 2026 Konrad Siuta. All rights reserved.
```

This repository is published solely as a **portfolio showcase** demonstrating software engineering, architectural, and development skills acquired as part of a Master's Thesis at the University of the National Education Commission in Krakow.

* **Viewing Permission:** The code, architecture diagrams, and documentation contained herein are made available for personal viewing and recruitment evaluation purposes only.
* **Restrictions:** No part of this source code or associated assets may be reproduced, distributed, sublicensed, modified, forked for commercial/academic submission, or used in derivative works without the explicit, written permission of the author (**Konrad Siuta**).
* **Academic Integrity:** Submitting any portion of this code or project as your own academic work is strictly prohibited and constitutes plagiarism under applicable institutional and legal frameworks.
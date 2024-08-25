
# PgUp - README

## Introduction

**PgUp** is a self-contained console application designed for seamless, zero-downtime deployment of PostgreSQL databases. PgUp's core philosophy centers around minimizing downtime and maintaining database integrity during complex deployments. It leverages PostgreSQL's advanced transactional capabilities to ensure atomicity and consistency, making it an essential tool for mission-critical environments where availability and reliability are paramount.

## Key Features

- **Zero-Downtime Deployment:** PgUp wraps all schema changes and data manipulations within PostgreSQL transactions, ensuring that either all changes are applied or none are, thus maintaining a consistent and reliable database state.
- **Automated Script Execution:** PgUp supports flexible script discovery and execution modes, allowing you to automate complex deployments without manual intervention. This reduces the risk of errors and ensures that changes are consistently applied across different environments.
- **Flexible Parameterization:** PgUp allows for the parameterization of SQL scripts, enabling you to easily adapt deployments to different environments (e.g., development, staging, production) without modifying the scripts themselves.
- **Cross-Platform Availability:** PgUp is available on multiple platforms, including Debian, Ubuntu, and Windows, ensuring that it can be installed and run in a variety of environments without requiring additional runtimes or dependencies.
- **Advanced Rollback Capabilities:** PgUp offers granular control over rollbacks, allowing you to configure rollback strategies on a per-transaction or per-batch basis, providing enhanced reliability in deployment processes.

## Installation

PgUp is available for installation on Debian, Ubuntu, and Windows using various package managers.

### Debian/Ubuntu Installation

To install PgUp on Debian or Ubuntu, use the following commands:

1. **Add the PgUp Repository:**
   ```bash
   sudo apt-get update
   ```

2. **Install PgUp:**
   ```bash
   sudo apt-get install pgup
   ```

### Windows Installation (Using Winget)

To install PgUp on Windows using Winget, follow these steps:

1. **Open Command Prompt or PowerShell.**

2. **Run the Winget Command:**
   ```bash
   winget install YourCompany.PgUp
   ```

This will download and install the latest version of PgUp on your system.

### Building from Source

If you prefer to build PgUp from source, you will need to have the .NET SDK and Git installed. The following steps outline the build process:

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/yourusername/pgup.git
   ```

2. **Build the Application:**
   ```bash
   cd pgup
   dotnet build
   ```

## Usage

PgUp provides a powerful and flexible interface for managing PostgreSQL database deployments. Below are some common examples of how to use the tool:

### Deploying a PostgreSQL Database

```bash
pgup deploy --project pgup.json --host localhost --username admin --password secret
```

This command deploys the database according to the specifications in the `pgup.json` configuration file.

### Managing Deployment Parameters

```bash
pgup deploy --project pgup.json --host localhost --username admin --password secret --parameter[dbName] my_custom_database
```

This command overrides the `dbName` parameter in the `pgup.json` file with a custom value for this deployment.

### Performing a Forced Overwrite

```bash
pgup deploy --project pgup.json --connection "Host=localhost;Username=postgres;Password=secret" --overwrite --force
```

This command forcefully overwrites the existing database, resulting in the loss of all current data.

For more detailed usage scenarios and advanced options, refer to the [Examples](#examples) section.

## Examples

Here are some practical examples of how PgUp can be utilized:

1. **Deploying with Custom Parameters:**
   ```bash
   pgup deploy --project pgup.json --host localhost --username admin --password secret --parameter[dbOwner] new_owner --timeout 00:30:00
   ```

2. **Rollback After Unit Testing:**
   PgUp allows you to define batches that run unit tests during the deployment process. These batches can be configured to roll back after execution, ensuring that test data does not persist in the production environment.

For additional examples and more complex scenarios, refer to the [examples.txt](examples.txt) file in the documentation.

## License

PgUp is licensed under the Mozilla Public License (MPL), which allows for the use, modification, and distribution of the software with the requirement that any modifications made to the source code are also shared under the same license. For more details, see the [LICENSE](LICENSE) file.

## Contributing

Contributions to PgUp are welcome! To contribute:

1. Fork the repository on GitHub.
2. Create a new branch for your feature or bugfix.
3. Submit a pull request with a detailed description of your changes.

Please ensure that your contributions adhere to the project's coding standards and guidelines.

## Support

For support, visit our [GitHub Issues](https://github.com/yourusername/pgup/issues) page or contact us via email at support@pgup.com.

## Acknowledgments

We would like to thank all contributors who have helped make PgUp a robust and reliable tool.

---

Thank you for choosing PgUp! We are confident it will streamline your PostgreSQL deployment processes and enhance your database management capabilities.

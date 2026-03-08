# Jules

Jules is a command-line tool for managing database schema changes through incremental SQL migrations. It helps track, apply, and roll back database changes in a consistent and controlled way.

```shell
         ________
       _/_  ___  \__
      /  | /   \ |  \      Jules v1.0.0
     |   ||     ||   |     ---------------
     |   | \___/ |   |     database migrations made easy.
      \_ |_______| _/
        \_________/
          |     |
        --[=====]--
```

## Overview

Jules manages schema changes by creating migration files containing SQL scripts. Each migration consists of:

* an **up** script: applies the schema change
* a **down** script: reverts the schema change

Jules tracks which migrations have been applied using a table in your database.

---

# Usage

## 1. Create a Configuration File

Every project using Jules requires a `jules.json` configuration file. This file contains the metadata Jules needs to interact with your database.

Generate it from the root directory of your project:

```shell
jules makeconfig
```

This creates a configuration file with the following structure:

```json
{
  "Dialect": "Sqlite",
  "DataSource": "Data Source=app.db",
  "Dir": "./Migrations"
}
```

### Configuration Fields

* **Dialect**
  Specifies the database type. Currently supported:

  * `psql`
  * `mssql`
  * `sqlite`

* **DataSource**
  The database connection string.

* **Dir**
  The directory where migration files will be stored.

---

## 2. Initialize Migration Tracking

Before running migrations, the database must be prepared to track them.

Run:

```shell
jules init
```

This command creates a table called `JulesMigrationsTracker` in your database.
The table records which migrations have been applied.

---

## 3. Create a Migration

To create a new migration, run:

```shell
jules create MyInitialMigration
```

This generates two files inside the migrations directory:

* **20260308214743__MyInitialMigration__up.sql**
  SQL script executed when applying the migration.

* **20260308214743__MyInitialMigration__down.sql**
  SQL script executed when reverting the migration.

The timestamp prefix ensures migrations are applied in chronological order.

---

## 4. Apply Migrations

After writing the SQL in your migration files, apply all pending migrations with:

```shell
jules up
```

This command executes all unapplied `up` migrations sequentially in ascending order.

---

## 5. Undo the Last Migration

If you need to revert the most recently applied migration, run:

```shell
jules undo
```

This executes the corresponding `down` migration script.

---

# CLI Reference

```shell
jules [COMMAND] [OPTIONS]

Commands:
  makeconfig   Scaffold the configuration file in the current directory
  init         Initialize the database for migration tracking
  create       Create a new migration in the migrations directory
  up           Apply pending migrations
  undo         Revert the last applied migration

Options:
  --showStackTrace   Show exception stack traces
```


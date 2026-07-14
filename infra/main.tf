variable "location" {
  description = "Azure region for the application resources."
  type        = string
  default     = "swedencentral"
}

variable "environment" {
  description = "Short environment identifier."
  type        = string
  default     = "dev"
}

resource "random_string" "suffix" {
  length  = 6
  upper   = false
  special = false
}

resource "azurerm_resource_group" "app" {
  name     = "rg-fire-monitor-${var.environment}-${random_string.suffix.result}"
  location = var.location
}

resource "azurerm_service_plan" "app" {
  name                = "plan-fire-monitor-${var.environment}"
  resource_group_name = azurerm_resource_group.app.name
  location            = azurerm_resource_group.app.location
  os_type             = "Linux"
  sku_name            = "B1"
}

resource "azurerm_linux_web_app" "api" {
  name                = "fire-monitor-${var.environment}-${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.app.name
  location            = azurerm_resource_group.app.location
  service_plan_id     = azurerm_service_plan.app.id
  https_only          = true

  identity { type = "SystemAssigned" }

  site_config {
    always_on = true
    application_stack { dotnet_version = "8.0" }
  }

  app_settings = {
    ASPNETCORE_ENVIRONMENT = "Production"
    DatabaseProvider       = "SqlServer"
  }
}

resource "azurerm_mssql_server" "app" {
  name                          = "sql-fire-monitor-${var.environment}-${random_string.suffix.result}"
  resource_group_name           = azurerm_resource_group.app.name
  location                      = azurerm_resource_group.app.location
  version                       = "12.0"
  minimum_tls_version           = "1.2"
  public_network_access_enabled = false

  azuread_administrator {
    login_username              = "fire-monitor-app"
    object_id                   = azurerm_linux_web_app.api.identity[0].principal_id
    azuread_authentication_only = true
  }
}

resource "azurerm_mssql_database" "app" {
  name      = "firemonitor"
  server_id = azurerm_mssql_server.app.id
  sku_name  = "Basic"
}

output "api_hostname" {
  value = azurerm_linux_web_app.api.default_hostname
}


# Get the current client configuration
data "azurerm_client_config" "current" {
}

resource "azurerm_resource_group" "iecag-infca" {
  name     = var.rgname
  location = var.location
}

resource "azurerm_log_analytics_workspace" "iecag-log-analytics" {
  name                = "iecag-log-analytics"
  location            = azurerm_resource_group.iecag-infca.location
  resource_group_name = azurerm_resource_group.iecag-infca.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_container_app_environment" "iecag-Environment" {
  name                       = "iecag-Environment"
  location                   = azurerm_resource_group.iecag-infca.location
  resource_group_name        = azurerm_resource_group.iecag-infca.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.iecag-log-analytics.id
}

resource "azurerm_container_app" "backend" {
  name                         = "backend"
  container_app_environment_id = azurerm_container_app_environment.iecag-Environment.id
  resource_group_name          = var.rgname
  revision_mode                = "Single"

  template {
    container {
      name   = "iecagserver"
      image  = var.server_image
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }
  registry {
    server               = var.ci_registry
    username             = var.ci_registry_user
    password_secret_name = "registry-secret"
  }

  secret {
    name  = "registry-secret"
    value = var.ci_registry_pw
  }
    
  ingress {
    transport    = "tcp"
    target_port  = 8008
    traffic_weight {
      percentage     = 100
      latest_revision = true
    }
    external_enabled = true
    allow_insecure_connections = true
    exposed_port = 8008  
  }
}

resource "azurerm_container_app" "web-app" {
  name                         = "web-app"
  container_app_environment_id = azurerm_container_app_environment.iecag-Environment.id
  resource_group_name          = var.rgname
  revision_mode                = "Single"

  template {
    container {
      name   = "iecagserver"
      image  = var.webapp_image
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }
 registry{
  server = var.ci_registry
  username = var.ci_registry_user
  password_secret_name = var.ci_registry_pw
  }
}

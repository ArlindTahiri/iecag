variable "location" {
  type        = string
  description = "The Azure region in which all resources will be created"
  default     = "West Europe"
}

variable "rgname" {
  type        = string
  description = "Name of the Resource Group"
  default     = "iecag-infca"
}

variable "client_secret" {}
variable "client_id" {}
variable "tenant_id" {}
variable "subscription_id" {}

variable "image" {}

variable "ci_registry" {}
variable "ci_registry_user" {}
variable "ci_registry_pw" {}



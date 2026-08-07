package io.licensemanager.sdk;

import com.fasterxml.jackson.annotation.JsonProperty;

public class InstallationResult {
    @JsonProperty("autorizado")   private boolean authorized;
    @JsonProperty("idInstalacao") private String installationId;
    @JsonProperty("jaRegistrada") private boolean alreadyRegistered;

    public boolean isAuthorized()       { return authorized; }
    public String getInstallationId()   { return installationId; }
    public boolean isAlreadyRegistered(){ return alreadyRegistered; }
}

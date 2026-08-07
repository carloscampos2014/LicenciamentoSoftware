package io.licensemanager.sdk;

import com.fasterxml.jackson.annotation.JsonProperty;

public class LoginResult {
    @JsonProperty("autorizado") private boolean authorized;
    @JsonProperty("idSessao")   private String sessionId;

    public boolean isAuthorized() { return authorized; }
    public String getSessionId()  { return sessionId; }
}

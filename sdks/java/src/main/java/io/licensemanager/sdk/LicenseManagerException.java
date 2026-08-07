package io.licensemanager.sdk;

public class LicenseManagerException extends Exception {
    private final int statusCode;
    private final String responseBody;

    public LicenseManagerException(int statusCode, String responseBody) {
        super("LicenseManager API error " + statusCode + ": " + responseBody);
        this.statusCode   = statusCode;
        this.responseBody = responseBody;
    }

    public int getStatusCode()     { return statusCode; }
    public String getResponseBody(){ return responseBody; }
}

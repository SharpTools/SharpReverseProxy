namespace SharpReverseProxy {
    public enum ProxyStatus {
        NotProxied,
        Proxied,
        NotAuthenticated,
        Cancelled,
        DownstreamServerError,
        ProxyError
    }
}
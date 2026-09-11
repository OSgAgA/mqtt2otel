window.getClientInfo = () => {
    return {
        userAgent: navigator.userAgent,
        platform: navigator.platform, 
        appName: navigator.appName,
        appVersion: navigator.appVersion,
        language: navigator.language ,
        windowWidth: window.innerWidth,
        windowHeight: window.innerHeight
    };
};  
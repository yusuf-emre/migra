# Download ChromeDriver
Invoke-WebRequest -Uri https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/119.0.6045.105/win32/chromedriver-win32.zip -OutFile chromedriver-win32.zip
Expand-Archive -Path chromedriver-win32.zip -DestinationPath .\drivers

# Download Chrome Binary
Invoke-WebRequest -Uri https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/119.0.6045.105/win32/chrome-win32.zip -OutFile chrome-win32.zip
Expand-Archive -Path chrome-win32.zip -DestinationPath .\drivers

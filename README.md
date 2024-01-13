# iot-sensor
azure iot hub sensor simulator

Below is a summary that you can use as a starting point for the README file. This template assumes that your application, referred to as `iot-sensor`, interacts with IoT devices and uploads a specified file to a designated service or location. Please customize the details to accurately reflect the specifics of your application and environment.  
   
---  
   
# IoT Sensor Data Uploader  
   
The `iot-sensor` application is designed to facilitate the uploading of sensor data from IoT devices to a central storage or processing service. The application runs within a Docker container and can be configured to upload a specific file from the host system or a pre-defined path within the container.  
   
## Features  
   
- Uploads a specified sensor data file to a configured service endpoint.
- Send telemetry data to Azure IoT hub  
- Configurable file path and name via runtime parameters.  
- Containerized deployment for easy scaling across different environments.  
   
## Prerequisites  
   
Before running the `iot-sensor` application, ensure you have the following installed:  
   
- Docker Engine  
- .NET runtime compatible with the application (if running outside of Docker)  
   
## Getting Started  
   
To get the application up and running, follow these steps:  
   
1. **Build the Docker Image**  
  
   Navigate to the project directory and run:  
  
   ```bash  
   docker build -t iot-sensor-app .  
   ```  
  
   This command builds the Docker image with the tag `iot-sensor`.  
   
2. **Run the Docker Container**  
  
   Use the following command to start the container, replacing `upload_me.txt` with the file you wish to upload:  
  
   ```bash  
   docker run --rm --env-file .env -e FILE_UPLOAD_PATH=/data/upload_me.txt -v "$(pwd)/data":/data --name iot-sensor-container iot-sensor 
   ```  
  
   - `--rm`: Removes the container after it exits.  
   - `--env-file .env`: Specifies the path to the environment variables file.  
   - `-e FILE_UPLOAD_PATH=upload_me.txt`: Sets the file path to the file to be uploaded.  
   - `-v "$(pwd)/data":/data`: Mounts the `data` directory in current working directory to the `/data` directory inside the container.  
   - `--name iot-sensor-container`: Assigns a name to the running container.  
   
3. **Environment Variables**  
  
   The application uses the following environment variables, which can be set in the `.env` file or directly in the `docker run` command:  
  
   - `FILE_UPLOAD_PATH`: Path to the file that will be uploaded by the application.  
   
4. **Volume Mounting**  
  
   To access files from the host system inside the container, mount the host directory to the container using the `-v` option.  
   
## Troubleshooting  
   
If you encounter the `FileNotFoundException`, ensure that:  
   
- The file `upload_me.txt` exists in the specified host directory.  
- The host directory is correctly mounted to the `/app` directory inside the container.  
- The `FILE_UPLOAD_PATH` environment variable is set correctly and points to the file to be uploaded.  
   
For more detailed logs and debugging, you can enter the container using:  
   
```bash  
docker exec -it iot-sensor-container /bin/bash  
```  
   
## Additional Information  
   
For more information on the Docker commands and options used, refer to the official Docker documentation.  
   
---  
   
Remember to replace placeholders (like `/path/to/host/directory`, `upload_me.txt`, and any other specific details) with the actual values relevant to your application. Make sure to provide any additional instructions or descriptions as needed to accurately represent your application's functionality and deployment process.
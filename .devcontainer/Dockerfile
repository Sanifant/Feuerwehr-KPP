FROM mcr.microsoft.com/devcontainers/dotnet:2-10.0

# install Java
# install essential tools
ARG JDK_VERSION=17
ENV JAVA_HOME=/usr/lib/jvm/java-${JDK_VERSION}-openjdk-amd64
RUN apt-get update && \
    apt-get dist-upgrade -y && \
    apt-get install -y --no-install-recommends openjdk-${JDK_VERSION}-jdk && \
    apt-get install -y --no-install-recommends git wget unzip && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

ARG ANDROID_SDK_VERSION=11076708
ENV ANDROID_HOME=/opt/android-sdk
RUN mkdir -p ${ANDROID_HOME}/cmdline-tools && \
    wget -q https://dl.google.com/android/repository/commandlinetools-linux-${ANDROID_SDK_VERSION}_latest.zip && \
    unzip *tools*linux*.zip -d ${ANDROID_HOME}/cmdline-tools && \
    mv ${ANDROID_HOME}/cmdline-tools/cmdline-tools ${ANDROID_HOME}/cmdline-tools/tools && \
    rm *tools*linux*.zip

ENV PATH=${PATH}:${ANDROID_HOME}/cmdline-tools/latest/bin:${ANDROID_HOME}/cmdline-tools/tools/bin:${ANDROID_HOME}/platform-tools:${ANDROID_HOME}/emulator
ADD license-accepter.sh /opt/
RUN chmod +x /opt/license-accepter.sh && /opt/license-accepter.sh $ANDROID_HOME

RUN dotnet workload install android
RUN dotnet tool install --ignore-failed-sources --add-source https://api.nuget.org/v3/index.json --global dotnet-ef
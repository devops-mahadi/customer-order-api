pipeline {
    agent any

    environment {
        DOTNET_CLI_HOME = "/tmp/dotnet"
        DOTNET_ROOT = "/usr/share/dotnet"
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "true"
        DOTNET_NOLOGO = "true"

        // Docker configuration
        DOCKER_REGISTRY = "docker.io"
        DOCKER_CREDENTIALS_ID = "dockerhub-credentials"
        CUSTOMERORDER_IMAGE = "customerorder-api"
        AUTHSERVICE_IMAGE = "authservice-api"

        // Version information
        BUILD_VERSION = "${env.BUILD_NUMBER}"
        GIT_SHORT_COMMIT = "${env.GIT_COMMIT?.take(7) ?: 'unknown'}"
    }

    parameters {
        string(name: 'BRANCH_NAME', defaultValue: 'main', description: 'Branch to build')
        booleanParam(name: 'RUN_INTEGRATION_TESTS', defaultValue: true, description: 'Run integration tests')
        booleanParam(name: 'COLLECT_COVERAGE', defaultValue: true, description: 'Collect code coverage')
        booleanParam(name: 'BUILD_DOCKER_IMAGES', defaultValue: true, description: 'Build Docker images')
        booleanParam(name: 'PUSH_DOCKER_IMAGES', defaultValue: false, description: 'Push Docker images to registry')
        string(name: 'DOCKER_HUB_USERNAME', defaultValue: '', description: 'Docker Hub username (leave empty to use credentials)')
        string(name: 'IMAGE_TAG', defaultValue: 'latest', description: 'Docker image tag (default: latest)')
    }

    stages {
        stage('Checkout') {
            steps {
                script {
                    echo "Checking out branch: ${params.BRANCH_NAME}"
                    checkout([
                        $class: 'GitSCM',
                        branches: [[name: "*/${params.BRANCH_NAME}"]],
                        userRemoteConfigs: [[url: env.GIT_URL]]
                    ])
                }
            }
        }

        stage('Display .NET Info') {
            steps {
                sh '''
                    dotnet --version
                    dotnet --list-sdks
                    dotnet --list-runtimes
                '''
            }
        }

        stage('Restore Dependencies') {
            steps {
                echo 'Restoring NuGet packages...'
                sh 'dotnet restore customerorder.sln'
            }
        }

        stage('Build Solution') {
            steps {
                echo 'Building solution...'
                sh 'dotnet build customerorder.sln --configuration Release --no-restore'
            }
        }

        stage('Run Unit Tests') {
            steps {
                echo 'Running unit tests...'
                script {
                    if (params.COLLECT_COVERAGE) {
                        sh '''
                            dotnet test CustomerOrder.Tests/CustomerOrder.Tests.csproj \
                                --configuration Release \
                                --no-build \
                                --filter "Category=Unit" \
                                --logger "trx;LogFileName=unit-tests.trx" \
                                --collect:"XPlat Code Coverage" \
                                --results-directory ./TestResults/Unit
                        '''
                    } else {
                        sh '''
                            dotnet test CustomerOrder.Tests/CustomerOrder.Tests.csproj \
                                --configuration Release \
                                --no-build \
                                --filter "Category=Unit" \
                                --logger "trx;LogFileName=unit-tests.trx" \
                                --results-directory ./TestResults/Unit
                        '''
                    }
                }
            }
        }

        stage('Run Integration Tests') {
            when {
                expression { params.RUN_INTEGRATION_TESTS == true }
            }
            steps {
                echo 'Running integration tests with in-memory database...'
                script {
                    if (params.COLLECT_COVERAGE) {
                        sh '''
                            dotnet test CustomerOrder.Tests/CustomerOrder.Tests.csproj \
                                --configuration Release \
                                --no-build \
                                --filter "Category=Integration" \
                                --logger "trx;LogFileName=integration-tests.trx" \
                                --collect:"XPlat Code Coverage" \
                                --results-directory ./TestResults/Integration
                        '''
                    } else {
                        sh '''
                            dotnet test CustomerOrder.Tests/CustomerOrder.Tests.csproj \
                                --configuration Release \
                                --no-build \
                                --filter "Category=Integration" \
                                --logger "trx;LogFileName=integration-tests.trx" \
                                --results-directory ./TestResults/Integration
                        '''
                    }
                }
            }
        }

        stage('Run All Tests') {
            when {
                expression { params.RUN_INTEGRATION_TESTS == false }
            }
            steps {
                echo 'Running all tests...'
                script {
                    if (params.COLLECT_COVERAGE) {
                        sh '''
                            dotnet test CustomerOrder.Tests/CustomerOrder.Tests.csproj \
                                --configuration Release \
                                --no-build \
                                --logger "trx;LogFileName=all-tests.trx" \
                                --collect:"XPlat Code Coverage" \
                                --results-directory ./TestResults/All
                        '''
                    } else {
                        sh '''
                            dotnet test CustomerOrder.Tests/CustomerOrder.Tests.csproj \
                                --configuration Release \
                                --no-build \
                                --logger "trx;LogFileName=all-tests.trx" \
                                --results-directory ./TestResults/All
                        '''
                    }
                }
            }
        }

        stage('Code Coverage Report') {
            when {
                expression { params.COLLECT_COVERAGE == true }
            }
            steps {
                echo 'Generating code coverage report...'
                sh '''
                    # Install ReportGenerator if not already installed
                    dotnet tool install --global dotnet-reportgenerator-globaltool || true

                    # Generate coverage report
                    ~/.dotnet/tools/reportgenerator \
                        -reports:"./TestResults/**/coverage.cobertura.xml" \
                        -targetdir:"./TestResults/CoverageReport" \
                        -reporttypes:"Html;Cobertura"
                '''
            }
        }

        stage('Publish Test Results') {
            steps {
                echo 'Publishing test results...'
                script {
                    // Publish test results
                    step([
                        $class: 'XUnitPublisher',
                        testTimeMargin: '3000',
                        thresholdMode: 1,
                        thresholds: [
                            [
                                $class: 'FailedThreshold',
                                failureNewThreshold: '',
                                failureThreshold: '0',
                                unstableNewThreshold: '',
                                unstableThreshold: ''
                            ]
                        ],
                        tools: [
                            [
                                $class: 'MSTestJunitHudsonTestType',
                                deleteOutputFiles: false,
                                failIfNotNew: false,
                                pattern: '**/TestResults/**/*.trx',
                                skipNoTestFiles: true,
                                stopProcessingIfError: false
                            ]
                        ]
                    ])

                    // Publish code coverage
                    if (params.COLLECT_COVERAGE) {
                        publishHTML([
                            allowMissing: false,
                            alwaysLinkToLastBuild: true,
                            keepAll: true,
                            reportDir: 'TestResults/CoverageReport',
                            reportFiles: 'index.html',
                            reportName: 'Code Coverage Report',
                            reportTitles: 'Code Coverage'
                        ])
                    }
                }
            }
        }

        stage('Build Docker Images') {
            when {
                expression { params.BUILD_DOCKER_IMAGES == true }
            }
            steps {
                script {
                    echo "Building Docker images..."
                    echo "Build Number: ${BUILD_VERSION}"
                    echo "Git Commit: ${GIT_SHORT_COMMIT}"
                    echo "Image Tag: ${params.IMAGE_TAG}"

                    // Determine Docker Hub username
                    def dockerHubUser = params.DOCKER_HUB_USERNAME ?: env.DOCKER_HUB_USERNAME ?: 'your-dockerhub-username'

                    // Build CustomerOrder API image
                    sh """
                        docker build \
                            -f CustomerOrder/Dockerfile \
                            -t ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:${params.IMAGE_TAG} \
                            -t ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:build-${BUILD_VERSION} \
                            -t ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:${GIT_SHORT_COMMIT} \
                            --build-arg BUILD_CONFIGURATION=Release \
                            --build-arg BUILD_VERSION=${BUILD_VERSION} \
                            --build-arg GIT_COMMIT=${GIT_SHORT_COMMIT} \
                            .
                    """

                    echo "CustomerOrder API image built successfully!"

                    // Build AuthService image
                    sh """
                        docker build \
                            -f AuthService/Dockerfile \
                            -t ${dockerHubUser}/${AUTHSERVICE_IMAGE}:${params.IMAGE_TAG} \
                            -t ${dockerHubUser}/${AUTHSERVICE_IMAGE}:build-${BUILD_VERSION} \
                            -t ${dockerHubUser}/${AUTHSERVICE_IMAGE}:${GIT_SHORT_COMMIT} \
                            --build-arg BUILD_CONFIGURATION=Release \
                            --build-arg BUILD_VERSION=${BUILD_VERSION} \
                            --build-arg GIT_COMMIT=${GIT_SHORT_COMMIT} \
                            .
                    """

                    echo "AuthService image built successfully!"

                    // Display built images
                    sh """
                        echo "=== Built Docker Images ==="
                        docker images | grep -E '${CUSTOMERORDER_IMAGE}|${AUTHSERVICE_IMAGE}' || true
                    """
                }
            }
        }

        stage('Test Docker Images') {
            when {
                expression { params.BUILD_DOCKER_IMAGES == true }
            }
            steps {
                script {
                    echo "Testing Docker images..."
                    def dockerHubUser = params.DOCKER_HUB_USERNAME ?: env.DOCKER_HUB_USERNAME ?: 'your-dockerhub-username'

                    // Test CustomerOrder image
                    sh """
                        echo "Testing CustomerOrder API image..."
                        docker run --rm ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:${params.IMAGE_TAG} dotnet --version || true
                    """

                    // Test AuthService image
                    sh """
                        echo "Testing AuthService image..."
                        docker run --rm ${dockerHubUser}/${AUTHSERVICE_IMAGE}:${params.IMAGE_TAG} dotnet --version || true
                    """

                    echo "Docker image tests completed!"
                }
            }
        }

        stage('Push Docker Images') {
            when {
                allOf {
                    expression { params.BUILD_DOCKER_IMAGES == true }
                    expression { params.PUSH_DOCKER_IMAGES == true }
                }
            }
            steps {
                script {
                    echo "Pushing Docker images to Docker Hub..."
                    def dockerHubUser = params.DOCKER_HUB_USERNAME ?: env.DOCKER_HUB_USERNAME ?: 'your-dockerhub-username'

                    // Login to Docker Hub
                    withCredentials([usernamePassword(
                        credentialsId: DOCKER_CREDENTIALS_ID,
                        usernameVariable: 'DOCKER_USER',
                        passwordVariable: 'DOCKER_PASS'
                    )]) {
                        sh 'echo $DOCKER_PASS | docker login -u $DOCKER_USER --password-stdin'
                    }

                    // Push CustomerOrder API images (all tags)
                    echo "Pushing CustomerOrder API images..."
                    sh """
                        docker push ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:${params.IMAGE_TAG}
                        docker push ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:build-${BUILD_VERSION}
                        docker push ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:${GIT_SHORT_COMMIT}
                    """

                    // Push AuthService images (all tags)
                    echo "Pushing AuthService images..."
                    sh """
                        docker push ${dockerHubUser}/${AUTHSERVICE_IMAGE}:${params.IMAGE_TAG}
                        docker push ${dockerHubUser}/${AUTHSERVICE_IMAGE}:build-${BUILD_VERSION}
                        docker push ${dockerHubUser}/${AUTHSERVICE_IMAGE}:${GIT_SHORT_COMMIT}
                    """

                    echo "Docker images pushed successfully!"

                    // Display pushed images
                    echo """
                    ========================================
                    Pushed Images:
                    ========================================
                    CustomerOrder API:
                      - ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:${params.IMAGE_TAG}
                      - ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:build-${BUILD_VERSION}
                      - ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:${GIT_SHORT_COMMIT}

                    AuthService:
                      - ${dockerHubUser}/${AUTHSERVICE_IMAGE}:${params.IMAGE_TAG}
                      - ${dockerHubUser}/${AUTHSERVICE_IMAGE}:build-${BUILD_VERSION}
                      - ${dockerHubUser}/${AUTHSERVICE_IMAGE}:${GIT_SHORT_COMMIT}
                    ========================================
                    """
                }
            }
        }

        stage('Cleanup Docker Images') {
            when {
                expression { params.BUILD_DOCKER_IMAGES == true }
            }
            steps {
                script {
                    echo "Cleaning up local Docker images..."
                    def dockerHubUser = params.DOCKER_HUB_USERNAME ?: env.DOCKER_HUB_USERNAME ?: 'your-dockerhub-username'

                    // Remove build-specific tags to save space
                    sh """
                        docker rmi ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:build-${BUILD_VERSION} || true
                        docker rmi ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:${GIT_SHORT_COMMIT} || true
                        docker rmi ${dockerHubUser}/${AUTHSERVICE_IMAGE}:build-${BUILD_VERSION} || true
                        docker rmi ${dockerHubUser}/${AUTHSERVICE_IMAGE}:${GIT_SHORT_COMMIT} || true
                    """

                    echo "Docker cleanup completed!"
                }
            }
        }
    }

    post {
        always {
            script {
                // Logout from Docker Hub
                sh 'docker logout || true'

                echo 'Cleaning up workspace...'
                cleanWs()
            }
        }
        success {
            script {
                echo '========================================='
                echo 'Pipeline completed successfully!'
                echo '========================================='

                if (params.BUILD_DOCKER_IMAGES && params.PUSH_DOCKER_IMAGES) {
                    def dockerHubUser = params.DOCKER_HUB_USERNAME ?: env.DOCKER_HUB_USERNAME ?: 'your-dockerhub-username'
                    echo """
                    Docker images are available at:
                    - https://hub.docker.com/r/${dockerHubUser}/${CUSTOMERORDER_IMAGE}
                    - https://hub.docker.com/r/${dockerHubUser}/${AUTHSERVICE_IMAGE}

                    Pull commands:
                    docker pull ${dockerHubUser}/${CUSTOMERORDER_IMAGE}:${params.IMAGE_TAG}
                    docker pull ${dockerHubUser}/${AUTHSERVICE_IMAGE}:${params.IMAGE_TAG}
                    """
                }
            }
        }
        failure {
            echo 'Pipeline failed!'
        }
        unstable {
            echo 'Pipeline unstable - some tests may have failed'
        }
    }
}

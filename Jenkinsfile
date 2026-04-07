pipeline {
    agent any

    environment {
        DOTNET_CLI_HOME = "/tmp/dotnet"
        DOTNET_ROOT = "/usr/share/dotnet"
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "true"
        DOTNET_NOLOGO = "true"
    }

    parameters {
        string(name: 'BRANCH_NAME', defaultValue: 'main', description: 'Branch to build')
        booleanParam(name: 'RUN_INTEGRATION_TESTS', defaultValue: true, description: 'Run integration tests')
        booleanParam(name: 'COLLECT_COVERAGE', defaultValue: true, description: 'Collect code coverage')
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
    }

    post {
        always {
            echo 'Cleaning up workspace...'
            cleanWs()
        }
        success {
            echo 'Pipeline completed successfully!'
        }
        failure {
            echo 'Pipeline failed!'
        }
        unstable {
            echo 'Pipeline unstable - some tests may have failed'
        }
    }
}

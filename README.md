## Build Microservices with AWS and .NET with ⚡ Resiliency: Polly + Circuit Breaker
The Search API Microservice
- Elasticsearch or network failures can happen intermittently.
- To prevent cascading failures, we implement:
  1. **Retry policy** – Retry a few times on transient failures.
  2. **Circuit breaker** – Stop calling Elasticsearch temporarily after repeated failures, allowing the system to recover.
Microservices with .NET and AWS 
![Description of image](2080118_8bbf_9.png)

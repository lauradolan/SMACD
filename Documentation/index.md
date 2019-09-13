<div style="margin: 2vh auto 0 auto;">
<img src="images/LogoHeader.png" alt="SMACD Logo" title="SMACD Logo" />
</div>

## Introduction to SMACD

SMACD (pronounced "smacked") is an *alternative process to conventional threat modeling applications*. This approach is driven by *mapping behaviors* that users can perform in your systems to *understand possible abuse patterns*.

The intent of SMACD is to *empower developers to test their own code for security vulnerabilities* and become more involved with their project's security roadmap. SMACD does this by uniting 3 common perspectives in a software organization to help map a system or application's potential vulnerabilities.

1. **Project management**, which provides features and use cases for the application from a design perspective. This perspective helps model how the system or application is expected to function.
2. **Product engineering**, which provides a list of target endpoints and inputs to implement the features and use cases. This perspective helps model how the system or application actually functions.
3. **Security engineering**, which brainstorms counter-use cases (possible abuse cases) in which attackers can abuse features in the system. This perspective helps model how the system or application's design differs from its implementation.

You can [get started with SMACD](articles/GetStarted.html) by using the CLI tool (**SMACD.Scanner**) or developing your own extensions to integrate with SMACD.

## Documentation

- [Get started with SMACD](articles/GetStarted.html)
- [SMACD data model](articles/DataModel.html)
- [SMACD scanner workflow](articles/ScannerWorkflow.html)

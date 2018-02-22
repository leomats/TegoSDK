Demonstrates how to connect to reader and perform synchronous and asynchronous execution.

Uses Xamarin Forms .NET Standard 2 code sharing strategy so all common code goes in the Example project
and the other three projects only need to be changed if adding platform specific code.

App.xaml.cs performs connection and disconnection.
MainPage.xaml.cs performs execution.

Output is simple single line in many cases without wrapping so generally works better on larger devices.

KNOWN ISSUES

Zeti (RFD8500) has an issue where reads or writes using multiple rounds are reported only once after execution.
Zeti (RFD8500) has a minimum select mask of nine bits.
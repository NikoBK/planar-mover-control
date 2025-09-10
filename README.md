# Work in Progress!
Thank you for checking in. Please note that this repository is currently a work in progress.\
Feel free to check back later or follow my blog (linked on the repository page) for updates.

# Dependencies
This project relies on a proprietary dependency:
- `PMCLIB.dll`, which can be downloaded [here](https://planarmotor.atlassian.net/wiki/pages/viewpageattachments.action?pageId=131043557&preview=%2F131043557%2F274203448%2FPMCLIB.dll) (_when downloaded, create a `libs` folder in the repo root dir and place the `dll` file there - I might make a shell script for this later_).

Other dependencies are:
- `log4net`: can be added using `dotnet add package log4net` (works for .NET Core 9.0 on linux)


# Usage & Install
This project is made for educational purposes. It demonstrates the usage and control range of the Acopos 6D planar motor controller (PMC) through modern C# with the [official convention](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names) from Microsoft applied in the code base which also includes some good practice for mover/shuttle behavior such as finite state machines that are defined and specified in XML files allowing runtime modification to these state machines to avoid the need for recompilation on changes. The main focus of this project is not only to demonstrate usage and control range, but also efficient handling that is timed, predictable to some degree and reliable.

# Credits
Special thanks to Associate Professor **Casper Schou** from Robotics & Automation at Aalborg University for allowing me to test this project on the AAU Matrix Table (B&R Acopos 6D) and supervising me on earlier semester projects that served as my introductions to Planar Motor Control in general. 

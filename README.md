# Work in Progress!
Thank you for checking in. Please note that this repository is currently a work in progress.\
Feel free to check back later or follow my blog (linked on the repository page) for updates.

# Dependencies
This project relies on a proprietary dependency that cannot be shared publicly due to licensing restrictions:\
`PMCLIB.dll`

A fallback implementation (mock controller) may be provided in the future to allow testing and exploration without this dependency. Other dependencies are:
- `log4net`: can be added using `dotnet add package log4net` (works for .NET Core 9.0 on linux)


# Disclaimer
This project is intended for educational purposes only. The aforementioned dependency (`PMCLIB.dll`) is treated as a black box to interface with the ACOPOS 6D table and as a result this project will be suitable for other students working with a similar Acopos 6D table by B\&R.

This project is unsupervised but follows the same conventions and licensing boundaries used in previous supervised semester projects with opensource repositories that used the same dependencies. This project does not provide documentation of methods, parameters, or interfaces from proprietary dependencies such as `PMCLIB.dll`.

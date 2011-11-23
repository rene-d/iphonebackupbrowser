#pragma once

// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the IBBSEARCH_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// IBBSEARCH_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef IBBSEARCH_EXPORTS
#define IBBSEARCH_API __declspec(dllexport)
#else
#define IBBSEARCH_API __declspec(dllimport)
#endif


#ifdef __cplusplus
extern "C" {
#endif


IBBSEARCH_API void __stdcall search(const wchar_t *filename, const wchar_t *pattern, wchar_t **results);


#ifdef __cplusplus
}
#endif

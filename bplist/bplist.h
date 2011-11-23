#pragma once

// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the BPLIST_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// BPLIST_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef BPLIST_EXPORTS
#define BPLIST_API __declspec(dllexport)
#else
#define BPLIST_API __declspec(dllimport)
#endif


#ifdef __cplusplus
extern "C" {
#endif

BPLIST_API int __stdcall mdinfo(const wchar_t *filename, wchar_t **Domain, wchar_t **Path);
BPLIST_API int __stdcall  bplist2xml_buffer(const char *byteArray, size_t length, char **xml, bool useOpenStepEpoch);
BPLIST_API int __stdcall  bplist2xml_buffer2(const char *byteArray, size_t length, char **xml, unsigned long flags);
BPLIST_API int __stdcall  bplist2xml_file(const wchar_t *filename, char **xml, bool useOpenStepEpoch);
BPLIST_API int __stdcall  bplist2xml_file2(const wchar_t *filename, char **xml, unsigned long flags);
BPLIST_API void __stdcall  bplist2xml_free(char *xml);

#ifdef __cplusplus
}
#endif

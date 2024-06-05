#ifndef OBJECT_H
#define OBJECT_H

#include <windows.h>

#ifdef OBJECT_EXPORTS
#define OBJECT_API __declspec(dllexport)
#else
#define OBJECT_API __declspec(dllimport)
#endif

extern "C" {
    OBJECT_API int loadModel(const char* filePath, void** objFile);
    OBJECT_API HBITMAP drawUVMap(void* objFile);
    OBJECT_API void deleteObject(void* objFile);
}

#endif // OBJECT_H


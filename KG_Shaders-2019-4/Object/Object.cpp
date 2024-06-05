#include "pch.h"
#include "Object.h"
#include <vector>
#include <fstream>
#include <sstream>
#include <string>

struct Vertex {
    float x, y, z;
};

struct UV {
    float u, v;
};

struct ObjFile {
    std::vector<Vertex> vertices;
    std::vector<UV> uvs;
    std::vector<int> vertexIndices;
    std::vector<int> uvIndices;
};

int loadModel(const char* filePath, void** objFile) {
    ObjFile* obj = new ObjFile();

    std::ifstream file(filePath);
    if (!file.is_open()) {
        return -1;
    }

    std::string line;
    while (std::getline(file, line)) {
        std::istringstream iss(line);
        std::string prefix;
        iss >> prefix;

        if (prefix == "v") {
            Vertex vertex;
            iss >> vertex.x >> vertex.y >> vertex.z;
            obj->vertices.push_back(vertex);
        }
        else if (prefix == "vt") {
            UV uv;
            iss >> uv.u >> uv.v;
            obj->uvs.push_back(uv);
        }
        else if (prefix == "f") {
            std::string vertexInfo;
            while (iss >> vertexInfo) {
                std::istringstream viss(vertexInfo);
                std::string indexStr;
                int vertexIndex, uvIndex;

                std::getline(viss, indexStr, '/');
                vertexIndex = std::stoi(indexStr);

                std::getline(viss, indexStr, '/');
                uvIndex = std::stoi(indexStr);

                obj->vertexIndices.push_back(vertexIndex - 1);
                obj->uvIndices.push_back(uvIndex - 1);
            }
        }
    }

    file.close();
    *objFile = obj;
    return 0;
}

HBITMAP drawUVMap(void* objFile) {
    ObjFile* obj = static_cast<ObjFile*>(objFile);
    const int width = 512;
    const int height = 512;

    HDC hdc = CreateCompatibleDC(NULL);
    HBITMAP hBitmap = CreateCompatibleBitmap(hdc, width, height);
    SelectObject(hdc, hBitmap);

    HBRUSH hBrush = CreateSolidBrush(RGB(255, 255, 255));
    RECT rect = { 0, 0, width, height };
    FillRect(hdc, &rect, hBrush);
    DeleteObject(hBrush);

    HPEN hPen = CreatePen(PS_SOLID, 1, RGB(0, 0, 0));
    SelectObject(hdc, hPen);

    for (size_t i = 0; i < obj->uvIndices.size(); i += 3) {
        UV uv1 = obj->uvs[obj->uvIndices[i]];
        UV uv2 = obj->uvs[obj->uvIndices[i + 1]];
        UV uv3 = obj->uvs[obj->uvIndices[i + 2]];

        POINT pt1 = { static_cast<LONG>(uv1.u * width), static_cast<LONG>((1.0f - uv1.v) * height) };
        POINT pt2 = { static_cast<LONG>(uv2.u * width), static_cast<LONG>((1.0f - uv2.v) * height) };
        POINT pt3 = { static_cast<LONG>(uv3.u * width), static_cast<LONG>((1.0f - uv3.v) * height) };

        MoveToEx(hdc, pt1.x, pt1.y, NULL);
        LineTo(hdc, pt2.x, pt2.y);
        LineTo(hdc, pt3.x, pt3.y);
        LineTo(hdc, pt1.x, pt1.y);
    }

    DeleteObject(hPen);
    DeleteDC(hdc);

    return hBitmap;
}

void deleteObject(void* objFile) {
    delete static_cast<ObjFile*>(objFile);
}





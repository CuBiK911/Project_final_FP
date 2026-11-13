#ifdef _WIN32
    #define EXPORT __declspec(dllexport)
#else
    #define EXPORT
#endif


EXPORT int calculate_experience_multiplier(int hours) {
    if (hours < 0) return -1;
    if (hours < 50) return 1;
    if (hours < 100) return 2;
    return 3;
}
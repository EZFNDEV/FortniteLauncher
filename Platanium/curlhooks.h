#pragma once
#include "curl.h"
#include "url.h"

CURLcode(*curl_setopt)(struct Curl_easy*, CURLoption, va_list) = nullptr;
CURLcode(*curl_easy_setopt)(struct Curl_easy*, CURLoption, ...) = nullptr;

CURLcode curl_setopt_(struct Curl_easy* data, CURLoption option, ...) {
    va_list arg;
    va_start(arg, option);

    CURLcode result = curl_setopt(data, option, arg);

    va_end(arg);
    return result;
}

CURLcode curl_easy_setopt_detour(struct Curl_easy* data, CURLoption tag, ...) {
    va_list arg;
    va_start(arg, tag);

    CURLcode result;

    if (!data)
        return CURLE_BAD_FUNCTION_ARGUMENT;

    if (tag == CURLOPT_SSL_VERIFYPEER) {
        result = curl_setopt_(data, tag, 0);
    }
    else {
        result = curl_setopt(data, tag, arg);
    }

    va_end(arg);
    return result;
}
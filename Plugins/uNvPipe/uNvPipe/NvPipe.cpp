#include <cstdint>
#include <IUnityRenderingExtensions.h>
#include "NvPipe.h"


namespace uNvPipe
{


bool Encoder::Initialize()
{
    const uint64_t bps = static_cast<uint64_t>(
        static_cast<double>(bitrateMbps_) * 1000 * 1000);

    const uint32_t encodeSize = width_ * height_ * 4;
    data_.resize(encodeSize);

    nvpipe_ = std::shared_ptr<::NvPipe>(
        NvPipe_CreateEncoder(
            format_, 
            codec_, 
            compression_, 
            bps, 
            targetFps_, 
            width_, 
            height_),
        [](auto* pPtr)
        {
            NvPipe_Destroy(pPtr);
        });

    return nvpipe_ != nullptr;
}


bool Encoder::Encode(const void *pData, bool forceIframe)
{
    if (!IsValid() || pData == nullptr) return false;

    size_ = static_cast<uint32_t>(
        NvPipe_Encode(
            nvpipe_.get(),
            pData,
            static_cast<uint64_t>(width_) * 4,
            data_.data(),
            data_.size(),
            width_,
            height_,
            forceIframe));

    return size_ != 0;
}


// ---


bool Decoder::Initialize()
{
    nvpipe_ = std::shared_ptr<::NvPipe>(
        NvPipe_CreateDecoder(
            format_, 
            codec_,
            width_, 
            height_),
        [](auto* pPtr)
        {
            NvPipe_Destroy(pPtr);
        });

    return nvpipe_ != nullptr;
}


bool Decoder::Decode(const void *pData, uint32_t size)
{
    if (!IsValid() || pData == nullptr) return false;

    std::unique_ptr<uint8_t[]> buf(new uint8_t[GetDecodedSize()]);

    const auto r = NvPipe_Decode(
        nvpipe_.get(),
        static_cast<const uint8_t*>(pData), 
        size,
        buf.get(), 
        width_, 
        height_);

    if (r != GetDecodedSize()) return false;

    {
        std::lock_guard<std::mutex> lock(dataMutex_);
        data_.emplace(latestDecodedIndex_++, std::move(buf));
    }

    return true;
}


const uint32_t Decoder::GetDecodedSize() const
{
    return width_ * height_ * 4;
}


const uint8_t * Decoder::GetDecodedData() const
{
    const auto it = data_.find(latestDecodedIndex_);
    return (it != data_.end()) ? it->second.get() : nullptr;
}


void Decoder::OnTextureUpdate(int eventId, void *pData)
{
    const auto event = static_cast<UnityRenderingExtEventType>(eventId);

    if (event == kUnityRenderingExtEventUpdateTextureBeginV2)
    {
        if (isUpdating_) return;

        const auto it = data_.find(textureIndex_);
        if (it == data_.end()) return;

        auto *pParams = static_cast<UnityRenderingExtTextureUpdateParamsV2*>(pData);
        if (width_ != pParams->width || 
            height_ != pParams->height)
        {
            return;
        }

        isUpdating_ = true;
        pParams->texData = it->second.get();
    }
    else if (event == kUnityRenderingExtEventUpdateTextureEndV2)
    {
        if (!isUpdating_) return;

        std::lock_guard<std::mutex> lock(dataMutex_);
        data_.erase(textureIndex_++);
        isUpdating_ = false;
    }
}


}
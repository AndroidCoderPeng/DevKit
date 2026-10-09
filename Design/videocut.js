/*
 * 视频裁剪 · 静态界面交互演示
 * 用于展示选择视频、预览播放、时间范围调整及模拟导出效果；不执行实际裁剪。
 */
(function () {
    'use strict';

    const $ = (selector) => document.querySelector(selector);
    const fileInput = $('#videoFileInput');
    const emptyState = $('#emptyState');
    const editorWorkspace = $('#editorWorkspace');
    const emptyError = $('#emptyError');
    const pageDescription = $('#pageDescription');
    const video = $('#previewVideo');
    const stage = $('.video-stage');
    const filmstrip = $('#filmstrip');
    const selection = $('.selection');
    const startInput = $('#start-time');
    const endInput = $('#end-time');
    const frameStep = 1 / 30;

    let duration = 84.6;
    let startTime = 8;
    let endTime = 52.5;
    let currentTime = 18.42;
    let objectUrl = null;
    let playbackTimer = null;
    let exportTimer = null;
    let dragging = null;

    function clamp(value, min, max) {
        return Math.min(max, Math.max(min, value));
    }

    function formatTime(seconds, milliseconds) {
        const safe = Math.max(0, Math.round((seconds || 0) * 1000) / 1000);
        const hours = Math.floor(safe / 3600);
        const minutes = Math.floor((safe % 3600) / 60);
        const wholeSeconds = Math.floor(safe % 60);
        const prefix = [hours, minutes, wholeSeconds]
            .map((part) => String(part).padStart(2, '0'))
            .join(':');
        if (!milliseconds) return prefix;
        const ms = String(Math.round((safe - Math.floor(safe)) * 1000)).padStart(3, '0');
        return prefix + '.' + ms;
    }

    function formatAxisTime(seconds) {
        const value = Math.max(0, Math.floor(seconds));
        if (value >= 3600) return formatTime(value, false);
        const minutes = String(Math.floor(value / 60)).padStart(2, '0');
        const remainder = String(value % 60).padStart(2, '0');
        return minutes + ':' + remainder;
    }

    function parseTime(value) {
        const parts = value.trim().split(':');
        if (parts.length !== 3) return NaN;
        const seconds = Number(parts[2]);
        const hours = Number(parts[0]);
        const minutes = Number(parts[1]);
        if ([hours, minutes, seconds].some(Number.isNaN) || minutes >= 60 || seconds >= 60 || hours < 0 || minutes < 0 || seconds < 0) {
            return NaN;
        }
        return hours * 3600 + minutes * 60 + seconds;
    }

    function viewport() {
        const visibleDuration = duration;
        const center = currentTime;
        const from = clamp(center - visibleDuration / 2, 0, Math.max(0, duration - visibleDuration));
        return { from: from, to: from + visibleDuration, span: visibleDuration };
    }

    function percentInViewport(time, view) {
        return clamp((time - view.from) / view.span * 100, 0, 100);
    }

    function render() {
        const view = viewport();
        const visibleStart = Math.max(startTime, view.from);
        const visibleEnd = Math.min(endTime, view.to);
        const selectionStart = percentInViewport(visibleStart, view);
        const selectionEnd = percentInViewport(visibleEnd, view);
        const playheadPosition = percentInViewport(currentTime, view);
        selection.style.left = selectionStart + '%';
        selection.style.right = (100 - selectionEnd) + '%';
        $('.playhead').style.left = playheadPosition + '%';

        $('#currentTime').textContent = formatTime(currentTime, true);
        $('#stageTime').textContent = formatTime(currentTime, true);
        $('#totalTime').textContent = formatTime(duration, true);
        $('#rangeStart').textContent = formatTime(startTime, true);
        $('#rangeEnd').textContent = formatTime(endTime, true);
        $('#rangeDuration').textContent = formatTime(endTime - startTime, true);
        $('#settingsDuration').textContent = formatTime(endTime - startTime, true);
        startInput.value = formatTime(startTime, true);
        endInput.value = formatTime(endTime, true);
        const ruler = $('.ruler');
        ruler.innerHTML = '';
        for (let i = 0; i <= 5; i++) {
            const tick = document.createElement('span');
            tick.textContent = formatAxisTime(view.from + view.span * i / 5);
            ruler.appendChild(tick);
        }
    }

    function updatePlayButtons(playing) {
        const label = playing ? '暂停' : '播放';
        $('#transportPlayButton').setAttribute('aria-label', label);
        $('#stagePlayButton').setAttribute('aria-label', label + '预览');
        const path = playing ? 'M7 5h3v14H7zM14 5h3v14h-3z' : 'M8 5.8v12.4L18 12 8 5.8z';
        $('#transportPlayButton svg').innerHTML = '<path d="' + path + '" fill="currentColor"/>';
        $('#stagePlayButton svg').innerHTML = '<path d="' + path + '" fill="currentColor"/>';
    }

    function stopPlayback() {
        clearInterval(playbackTimer);
        playbackTimer = null;
        if (!video.paused) video.pause();
        updatePlayButtons(false);
    }

    function seek(time) {
        currentTime = clamp(time, 0, duration);
        if (video.src) video.currentTime = currentTime;
        render();
    }

    function togglePlayback() {
        if (playbackTimer || (!video.paused && !video.ended)) {
            stopPlayback();
            return;
        }

        if (currentTime < startTime || currentTime >= endTime) seek(startTime);
        updatePlayButtons(true);
        if (video.src) {
            video.play().catch(function () {
                updatePlayButtons(false);
            });
        } else {
            playbackTimer = setInterval(function () {
                currentTime += 0.1;
                if (currentTime >= endTime) {
                    currentTime = startTime;
                }
                render();
            }, 100);
        }
    }

    function timeFromPointer(event) {
        const rect = filmstrip.getBoundingClientRect();
        const ratio = clamp((event.clientX - rect.left) / rect.width, 0, 1);
        const view = viewport();
        return view.from + ratio * view.span;
    }

    function beginDrag(event, edge) {
        event.preventDefault();
        dragging = edge;
        event.currentTarget.setPointerCapture(event.pointerId);
    }

    $('.handle-left').addEventListener('pointerdown', (event) => beginDrag(event, 'start'));
    $('.handle-right').addEventListener('pointerdown', (event) => beginDrag(event, 'end'));
    filmstrip.addEventListener('pointermove', function (event) {
        if (!dragging) return;
        const value = timeFromPointer(event);
        if (dragging === 'start') {
            startTime = clamp(value, 0, endTime - frameStep);
        } else {
            endTime = clamp(value, startTime + frameStep, duration);
        }
        if (currentTime < startTime || currentTime > endTime) seek(dragging === 'start' ? startTime : endTime);
        else render();
    });
    filmstrip.addEventListener('pointerup', () => { dragging = null; });
    filmstrip.addEventListener('pointercancel', () => { dragging = null; });
    filmstrip.addEventListener('click', function (event) {
        if (event.target.closest('.handle')) return;
        seek(timeFromPointer(event));
    });

    function chooseVideo() {
        fileInput.click();
    }

    function showFileError(message) {
        emptyError.textContent = message;
        emptyError.hidden = false;
    }

    function loadVideoFile(file) {
        if (!file) return;
        if (!file.name.toLowerCase().endsWith('.mp4')) {
            showFileError('暂不支持该格式，请选择 MP4 文件。');
            return;
        }

        emptyError.hidden = true;
        emptyState.hidden = true;
        editorWorkspace.hidden = false;
        pageDescription.textContent = '拖动时间轴两端的手柄，选择需要保留的片段。';

        if (objectUrl) URL.revokeObjectURL(objectUrl);
        objectUrl = URL.createObjectURL(file);
        video.src = objectUrl;
        stage.classList.add('has-video');
        $('.file-name').textContent = file.name;
        $('.file-meta').textContent = (file.type || 'MP4 视频') + ' · 正在读取媒体信息';
        $('#outputPath').textContent = file.name.replace(/\.[^.]+$/, '') + '_裁剪.mp4';

        video.addEventListener('loadedmetadata', function onMetadata() {
            video.removeEventListener('loadedmetadata', onMetadata);
            duration = Number.isFinite(video.duration) ? video.duration : duration;
            endTime = Math.min(endTime, duration);
            startTime = Math.min(startTime, Math.max(0, endTime - frameStep));
            currentTime = startTime;
            $('.file-meta').textContent = 'MP4 · ' + Math.round(video.videoWidth) + ' × ' + Math.round(video.videoHeight) + ' · ' + formatTime(duration, false);
            render();
        });
        video.addEventListener('error', function onError() {
            video.removeEventListener('error', onError);
            editorWorkspace.hidden = true;
            emptyState.hidden = false;
            stage.classList.remove('has-video');
            showFileError('无法读取此视频，请确认文件未损坏后重试。');
        });
    }

    $('#chooseVideoButton').addEventListener('click', chooseVideo);
    $('#changeVideo').addEventListener('click', chooseVideo);
    fileInput.addEventListener('change', function () {
        loadVideoFile(fileInput.files && fileInput.files[0]);
    });

    emptyState.addEventListener('dragover', function (event) {
        event.preventDefault();
        emptyState.classList.add('drag-over');
    });
    emptyState.addEventListener('dragleave', function (event) {
        if (!emptyState.contains(event.relatedTarget)) emptyState.classList.remove('drag-over');
    });
    emptyState.addEventListener('drop', function (event) {
        event.preventDefault();
        emptyState.classList.remove('drag-over');
        loadVideoFile(event.dataTransfer.files && event.dataTransfer.files[0]);
    });

    $('#stagePlayButton').addEventListener('click', togglePlayback);
    $('#transportPlayButton').addEventListener('click', togglePlayback);
    video.addEventListener('play', () => updatePlayButtons(true));
    video.addEventListener('pause', () => updatePlayButtons(false));
    video.addEventListener('timeupdate', function () {
        currentTime = video.currentTime;
        if (currentTime >= endTime) {
            video.pause();
            video.currentTime = endTime;
            currentTime = endTime;
        }
        render();
    });
    $('#previousFrame').addEventListener('click', () => seek(currentTime - frameStep));
    $('#nextFrame').addEventListener('click', () => seek(currentTime + frameStep));
    startInput.addEventListener('change', function () {
        const parsed = parseTime(startInput.value);
        if (!Number.isNaN(parsed)) startTime = clamp(parsed, 0, endTime - frameStep);
        render();
    });
    endInput.addEventListener('change', function () {
        const parsed = parseTime(endInput.value);
        if (!Number.isNaN(parsed)) endTime = clamp(parsed, startTime + frameStep, duration);
        render();
    });

    $('#changeOutput').addEventListener('click', function () {
        const path = window.prompt('输入演示用的输出文件路径', $('#outputPath').textContent);
        if (path && path.trim()) $('#outputPath').textContent = path.trim();
    });

    $('#startTrim').addEventListener('click', function () {
        const button = $('#startTrim');
        const text = $('#trimButtonText');
        if (exportTimer) return;
        button.disabled = true;
        let progress = 0;
        text.textContent = '准备裁剪…';
        exportTimer = setInterval(function () {
            progress = Math.min(100, progress + 4);
            text.textContent = '裁剪中 ' + progress + '%';
            if (progress >= 100) {
                clearInterval(exportTimer);
                exportTimer = null;
                text.textContent = '演示完成';
                button.disabled = false;
                setTimeout(function () { text.textContent = '开始裁剪'; }, 1500);
            }
        }, 90);
    });

    render();
})();

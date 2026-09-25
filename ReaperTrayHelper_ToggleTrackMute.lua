-- REAPER Tray Helper bridge: invoked through REAPER's local OSC ACTION pattern.
-- Requests are single-use files written by ReaperTrayHelper.exe.

local local_app_data = os.getenv("LOCALAPPDATA")
if not local_app_data or local_app_data == "" then return end

local separator = package.config:sub(1, 1)
local bridge = local_app_data .. separator .. "ReaperTrayHelper" .. separator .. "bridge"

local function write_response(path, code, value)
  local temporary = path .. ".tmp"
  local file = io.open(temporary, "wb")
  if not file then return end
  file:write(code, "\n", value or "")
  file:close()
  os.rename(temporary, path)
end

local function process_request(path, id)
  local file = io.open(path, "rb")
  if not file then return end
  local operation = file:read("*l")
  local expires = tonumber(file:read("*l"))
  local target_name = file:read("*a") or ""
  file:close()

  if not id then return end
  local response_path = bridge .. separator .. id .. ".res"

  if not expires or os.time() > expires then
    write_response(response_path, "EXPIRED", "")
    return
  end
  if operation == "PING" then
    write_response(response_path, "PING_OK", "")
    return
  end
  if operation ~= "TOGGLE" or target_name == "" then
    write_response(response_path, "BAD_REQUEST", "")
    return
  end

  local project = reaper.EnumProjects(-1, "")
  if not project then
    write_response(response_path, "NO_PROJECT", "")
    return
  end

  local match = nil
  local matches = 0
  local count = reaper.CountTracks(project)
  for index = 0, count - 1 do
    local track = reaper.GetTrack(project, index)
    local _, name = reaper.GetTrackName(track)
    if name == target_name then
      match = track
      matches = matches + 1
    end
  end

  if matches == 0 then
    write_response(response_path, "NO_TRACK", "")
    return
  end
  if matches > 1 then
    write_response(response_path, "DUPLICATE_TRACK", "")
    return
  end

  local state = reaper.SetTrackUIMute(match, -1, 3)
  if state == nil or state < 0 then
    write_response(response_path, "TRACK_API_ERROR", "")
    return
  end
  write_response(response_path, state > 0 and "MUTED" or "UNMUTED", "")
end

local requests = {}
reaper.EnumerateFiles(bridge, -1)
local index = 0
while true do
  local name = reaper.EnumerateFiles(bridge, index)
  if not name then break end
  if name:match("%.req$") then
    requests[#requests + 1] = { path = bridge .. separator .. name, id = name:sub(1, -5) }
  end
  index = index + 1
end

table.sort(requests)
for _, path in ipairs(requests) do
  process_request(path.path, path.id)
end

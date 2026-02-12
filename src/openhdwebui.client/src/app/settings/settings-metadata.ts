export type SettingValueType = 'boolean' | 'number' | 'string';

export interface SettingFieldOption {
  value: string | number | boolean;
  label: string;
  description?: string;
}

export interface SettingFieldMeta {
  label: string;
  description?: string;
  control?: 'toggle' | 'select' | 'number' | 'text';
  valueType?: SettingValueType;
  options?: readonly SettingFieldOption[] | SettingFieldOption[];
  min?: number;
  max?: number;
  step?: number;
  unit?: string;
  group?: string;
}

export interface SettingsFileMeta {
  title?: string;
  fields: Record<string, SettingFieldMeta>;
  order?: string[];
  groupOrder?: string[];
}

export const CAMERA_TYPE_OPTIONS = [
  { value: 0, label: 'Dummy' },
  { value: 2, label: 'External' },
  { value: 3, label: 'External IP' },
  { value: 4, label: 'Development file source' },
  { value: 10, label: 'USB camera (software encode)' },
  { value: 11, label: 'InfiRay thermal' },
  { value: 12, label: 'InfiRay T2' },
  { value: 13, label: 'InfiRay X2' },
  { value: 14, label: 'InfiRay P2 Pro' },
  { value: 15, label: 'FLIR Vue' },
  { value: 16, label: 'FLIR Boson' },
  { value: 20, label: 'Raspberry Pi HDMI to CSI' },
  { value: 30, label: 'Raspberry Pi cam v1 (OV5647)' },
  { value: 31, label: 'Raspberry Pi cam v2 (IMX219)' },
  { value: 32, label: 'Raspberry Pi cam v3 (IMX708)' },
  { value: 33, label: 'Raspberry Pi HQ (IMX477)' },
  { value: 40, label: 'ArduCam Skymaster HDR' },
  { value: 41, label: 'ArduCam Skyvision Pro' },
  { value: 42, label: 'ArduCam IMX477M' },
  { value: 43, label: 'ArduCam IMX462' },
  { value: 44, label: 'ArduCam IMX327' },
  { value: 45, label: 'ArduCam IMX290' },
  { value: 46, label: 'ArduCam IMX462 Low-Light Mini' },
  { value: 60, label: 'VEYE 2MP' },
  { value: 61, label: 'VEYE IMX307' },
  { value: 62, label: 'VEYE CSSC132' },
  { value: 63, label: 'VEYE MVCAM' },
  { value: 70, label: 'HDZero generic (X20)' },
  { value: 71, label: 'HDZero RunCam V1 (X20)' },
  { value: 72, label: 'HDZero RunCam V2 (X20)' },
  { value: 73, label: 'HDZero RunCam V3 (X20)' },
  { value: 74, label: 'HDZero RunCam Nano 90 (X20)' },
  { value: 75, label: 'OHD Jaguar (X20)' },
  { value: 76, label: 'OHD Jaguar (X21)' },
  { value: 80, label: 'Rock 5 HDMI input' },
  { value: 81, label: 'Rock 5 OV5647' },
  { value: 82, label: 'Rock 5 IMX219' },
  { value: 83, label: 'Rock 5 IMX708' },
  { value: 84, label: 'Rock 5 IMX462' },
  { value: 85, label: 'Rock 5 IMX415' },
  { value: 86, label: 'Rock 5 IMX477' },
  { value: 87, label: 'Rock 5 IMX519' },
  { value: 88, label: 'Rock 5 OHD Jaguar' },
  { value: 90, label: 'Rock 3 HDMI input' },
  { value: 91, label: 'Rock 3 OV5647' },
  { value: 92, label: 'Rock 3 IMX219' },
  { value: 93, label: 'Rock 3 IMX708' },
  { value: 94, label: 'Rock 3 IMX462' },
  { value: 95, label: 'Rock 3 IMX519' },
  { value: 96, label: 'Rock 3 OHD Jaguar' },
  { value: 97, label: 'Rock 3 VEYE' },
  { value: 101, label: 'NVIDIA Xavier IMX577' },
  { value: 110, label: 'OpenIPC generic' },
  { value: 120, label: 'Coretronic IMX577' },
  { value: 121, label: 'Coretronic OV9282' },
  { value: 122, label: 'Willy Hornet' },
  { value: 123, label: 'Willy Jaguar' },
  { value: 124, label: 'Willy Rekindle' },
  { value: 255, label: 'Disabled' }
] as const;

const UART_BAUD_OPTIONS: readonly SettingFieldOption[] = [
  9600,
  19200,
  38400,
  57600,
  115200,
  230400,
  460800,
  500000,
  576000,
  921600,
  1000000
].map(value => ({ value, label: `${value.toLocaleString()} baud` }));

const WIFI_HOTSPOT_OPTIONS: readonly SettingFieldOption[] = [
  { value: 0, label: 'Automatic (disable when armed)' },
  { value: 1, label: 'Always off' },
  { value: 2, label: 'Always on' }
];

const ETHERNET_MODE_OPTIONS: readonly SettingFieldOption[] = [
  { value: 0, label: 'Leave ethernet untouched' },
  { value: 1, label: 'Hotspot mode (static IP)' },
  { value: 2, label: 'Forward to external uplink' }
];

const BOOLEAN_SELECT_OPTIONS: readonly SettingFieldOption[] = [
  { value: 0, label: 'Disabled' },
  { value: 1, label: 'Enabled' }
];

export const SETTINGS_METADATA: Record<string, SettingsFileMeta> = {
  'interface/networking_settings.json': {
    title: 'Networking',
    groupOrder: ['Interfaces'],
    order: [
      'wifi_hotspot_mode',
      'wifi_hotspot_interface_override',
      'wifi_hotspot_ssid',
      'wifi_hotspot_password',
      'ethernet_operating_mode'
    ],
    fields: {
      wifi_hotspot_mode: {
        label: 'WiFi hotspot mode',
        description: 'Control whether the OpenHD hotspot is managed automatically, forced off or always on.',
        control: 'select',
        valueType: 'number',
        options: WIFI_HOTSPOT_OPTIONS,
        group: 'Interfaces'
      },
      wifi_hotspot_interface_override: {
        label: 'WiFi hotspot interface override',
        description: 'Optional interface name to force for hotspot mode (leave blank for auto).',
        control: 'text',
        valueType: 'string',
        group: 'Interfaces'
      },
      wifi_hotspot_ssid: {
        label: 'WiFi hotspot SSID',
        description: 'Override the default OpenHD hotspot SSID (blank uses the unit name).',
        control: 'text',
        valueType: 'string',
        group: 'Interfaces'
      },
      wifi_hotspot_password: {
        label: 'WiFi hotspot password',
        description: 'Override the default hotspot password (8–63 chars; blank uses default).',
        control: 'text',
        valueType: 'string',
        group: 'Interfaces'
      },
      ethernet_operating_mode: {
        label: 'Ethernet operating mode',
        description: 'Choose how OpenHD configures the ethernet port (untouched, hotspot or forwarding).',
        control: 'select',
        valueType: 'number',
        options: ETHERNET_MODE_OPTIONS,
        group: 'Interfaces'
      }
    }
  },
  'telemetry/air_settings.json': {
    title: 'Telemetry (air unit)',
    groupOrder: ['Flight controller UART', 'Battery'],
    order: ['fc_uart_connection_type', 'fc_uart_baudrate', 'fc_uart_flow_control', 'fc_battery_n_cells'],
    fields: {
      fc_uart_connection_type: {
        label: 'Flight controller UART port',
        description: 'Select which serial device OpenHD should use to communicate with the flight controller. "DEFAULT" resolves to the recommended port for the current platform.',
        control: 'select',
        valueType: 'string',
        options: [
          { value: 'DEFAULT', label: 'Auto-detect (platform default)' },
          { value: '', label: 'Disabled' },
          { value: '/dev/serial0', label: 'Raspberry Pi /dev/serial0' },
          { value: '/dev/serial1', label: 'Raspberry Pi /dev/serial1' },
          { value: '/dev/ttyUSB0', label: 'USB adapter /dev/ttyUSB0' },
          { value: '/dev/ttyUSB1', label: 'USB adapter /dev/ttyUSB1' },
          { value: '/dev/ttyACM0', label: 'USB flight controller /dev/ttyACM0' },
          { value: '/dev/ttyACM1', label: 'USB flight controller /dev/ttyACM1' },
          { value: '/dev/ttyS7', label: 'Rock 5B /dev/ttyS7' },
          { value: '/dev/ttyS2', label: 'Rock /dev/ttyS2' }
        ],
        group: 'Flight controller UART'
      },
      fc_uart_baudrate: {
        label: 'UART baud rate',
        description: 'Allowed values follow the Linux serial driver (9 600 – 1 000 000 baud).',
        control: 'select',
        valueType: 'number',
        options: UART_BAUD_OPTIONS,
        group: 'Flight controller UART'
      },
      fc_uart_flow_control: {
        label: 'Hardware flow control (RTS/CTS)',
        description: 'Enable RTS/CTS flow control for flight-controller serial links that support it.',
        control: 'toggle',
        valueType: 'boolean',
        group: 'Flight controller UART'
      },
      fc_battery_n_cells: {
        label: 'Battery cell count',
        description: 'Set the number of cells reported by the flight controller (0 leaves it unset).',
        control: 'number',
        valueType: 'number',
        min: 0,
        max: 12,
        group: 'Battery'
      }
    }
  },
  'video/air_camera_generic.json': {
    title: 'Air video',
    groupOrder: ['Cameras', 'Dual camera', 'Audio'],
    order: [
      'primary_camera_type',
      'secondary_camera_type',
      'switch_primary_and_secondary',
      'dualcam_primary_video_allocated_bandwidth_perc',
      'enable_audio'
    ],
    fields: {
      primary_camera_type: {
        label: 'Primary camera type',
        description: 'Matches the camera enumeration used by OpenHD (camera.hpp).',
        control: 'select',
        valueType: 'number',
        options: CAMERA_TYPE_OPTIONS,
        group: 'Cameras'
      },
      secondary_camera_type: {
        label: 'Secondary camera type',
        description: 'Set to "Disabled" to keep the secondary stream inactive.',
        control: 'select',
        valueType: 'number',
        options: CAMERA_TYPE_OPTIONS,
        group: 'Cameras'
      },
      switch_primary_and_secondary: {
        label: 'Swap primary and secondary inputs',
        description: 'Swap detected camera order when the hardware enumeration is inverted.',
        control: 'toggle',
        valueType: 'boolean',
        group: 'Cameras'
      },
      dualcam_primary_video_allocated_bandwidth_perc: {
        label: 'Primary camera bandwidth share (%)',
        description: 'Allocate bitrate between primary and secondary streams (10% – 90%).',
        control: 'number',
        valueType: 'number',
        min: 10,
        max: 90,
        step: 5,
        group: 'Dual camera',
        unit: '%'
      },
      enable_audio: {
        label: 'Audio capture mode',
        description: 'Enable real audio capture or run the encoder test tone.',
        control: 'select',
        valueType: 'number',
        options: [
          { value: 1, label: 'Disabled' },
          { value: 0, label: 'Capture live audio' },
          { value: 100, label: 'Enable test tone output' }
        ],
        group: 'Audio'
      }
    }
  },
  'interface/wifibroadcast_settings.json': {
    title: 'WiFi broadcast link',
    groupOrder: ['Radio link', 'Transmission power', 'FEC & encoding', 'RC integration', 'Advanced'],
    order: [
      'wb_frequency',
      'wb_air_tx_channel_width',
      'wb_air_mcs_index',
      'wb_enable_stbc',
      'wb_enable_ldpc',
      'wb_enable_short_guard',
      'enable_wb_video_variable_bitrate',
      'wb_tx_power_milli_watt',
      'wb_tx_power_milli_watt_armed',
      'wb_rtl8812au_tx_pwr_idx_override',
      'wb_rtl8812au_tx_pwr_idx_override_armed',
      'wb_video_fec_percentage',
      'wb_video_rate_for_mcs_adjustment_percent',
      'wb_max_fec_block_size',
      'wb_qp_min',
      'wb_qp_max',
      'wb_mcs_index_via_rc_channel',
      'wb_bw_via_rc_channel',
      'wb_enable_listen_only_mode',
      'wb_dev_air_set_high_retransmit_count'
    ],
    fields: {
      wb_frequency: {
        label: 'Center frequency (MHz)',
        description: 'Must be a supported WiFi channel frequency. OpenHD validates against the card capabilities.',
        control: 'number',
        valueType: 'number',
        min: 2300,
        max: 5900,
        step: 1,
        unit: 'MHz',
        group: 'Radio link'
      },
      wb_air_tx_channel_width: {
        label: 'Channel width (MHz)',
        description: 'Valid channel widths are 10, 20 or 40 MHz.',
        control: 'select',
        valueType: 'number',
        options: [
          { value: 10, label: '10 MHz' },
          { value: 20, label: '20 MHz' },
          { value: 40, label: '40 MHz' }
        ],
        group: 'Radio link'
      },
      wb_air_mcs_index: {
        label: 'Air unit MCS index',
        description: '0 – 31 per validate_settings_helper.h; lower values trade throughput for range.',
        control: 'number',
        valueType: 'number',
        min: 0,
        max: 31,
        group: 'Radio link'
      },
      wb_enable_stbc: {
        label: 'Enable STBC',
        description: 'Space-time block coding can improve diversity on multi-antenna hardware.',
        control: 'select',
        valueType: 'number',
        options: BOOLEAN_SELECT_OPTIONS,
        group: 'Radio link'
      },
      wb_enable_ldpc: {
        label: 'Enable LDPC',
        description: 'Turn on low-density parity-check coding when supported by your radios.',
        control: 'toggle',
        valueType: 'boolean',
        group: 'Radio link'
      },
      wb_enable_short_guard: {
        label: 'Use short guard interval',
        description: 'Short guard intervals slightly reduce robustness but increase throughput.',
        control: 'toggle',
        valueType: 'boolean',
        group: 'Radio link'
      },
      enable_wb_video_variable_bitrate: {
        label: 'Allow variable encoder bitrate',
        description: 'Let the link dynamically request lower encoder bitrates on poor links.',
        control: 'toggle',
        valueType: 'boolean',
        group: 'Radio link'
      },
      wb_tx_power_milli_watt: {
        label: 'TX power (mW)',
        description: 'Valid range 10 – 30 000 mW per validate_settings_helper.h.',
        control: 'number',
        valueType: 'number',
        min: 10,
        max: 30000,
        group: 'Transmission power',
        unit: 'mW'
      },
      wb_tx_power_milli_watt_armed: {
        label: 'TX power when armed (mW)',
        description: 'Override transmit power when the vehicle is armed (0 keeps the default).',
        control: 'number',
        valueType: 'number',
        min: 0,
        max: 30000,
        group: 'Transmission power',
        unit: 'mW'
      },
      wb_rtl8812au_tx_pwr_idx_override: {
        label: 'RTL8812AU TX power index',
        description: 'Valid range 0 – 63.',
        control: 'number',
        valueType: 'number',
        min: 0,
        max: 63,
        group: 'Transmission power'
      },
      wb_rtl8812au_tx_pwr_idx_override_armed: {
        label: 'RTL8812AU TX power index when armed',
        description: 'Set to 0 to keep the default armed power index.',
        control: 'number',
        valueType: 'number',
        min: 0,
        max: 63,
        group: 'Transmission power'
      },
      wb_video_fec_percentage: {
        label: 'Video FEC percentage',
        description: 'Forward-error correction ratio (1 – 400%).',
        control: 'number',
        valueType: 'number',
        min: 1,
        max: 400,
        step: 5,
        group: 'FEC & encoding',
        unit: '%'
      },
      wb_video_rate_for_mcs_adjustment_percent: {
        label: 'Rate reduction for MCS (%)',
        description: 'Reduce encoder bitrate relative to theoretical maximum (10 – 200%).',
        control: 'number',
        valueType: 'number',
        min: 10,
        max: 200,
        step: 5,
        group: 'FEC & encoding',
        unit: '%'
      },
      wb_max_fec_block_size: {
        label: 'Max FEC block size',
        description: '0 enables automatic sizing, positive values clamp the block size. -1 keeps the platform default.',
        control: 'number',
        valueType: 'number',
        min: -1,
        max: 99,
        group: 'FEC & encoding'
      },
      wb_qp_min: {
        label: 'Encoder QP minimum',
        description: 'Lower values improve quality but need more bandwidth (typical range 10 – 45).',
        control: 'number',
        valueType: 'number',
        min: 0,
        max: 51,
        group: 'FEC & encoding'
      },
      wb_qp_max: {
        label: 'Encoder QP maximum',
        description: 'Higher values allow more compression at the cost of quality.',
        control: 'number',
        valueType: 'number',
        min: 0,
        max: 51,
        group: 'FEC & encoding'
      },
      wb_mcs_index_via_rc_channel: {
        label: 'RC channel for MCS control',
        description: '0 disables RC control. Otherwise set the RC channel index that should modify MCS.',
        control: 'number',
        valueType: 'number',
        min: 0,
        max: 16,
        group: 'RC integration'
      },
      wb_bw_via_rc_channel: {
        label: 'RC channel for bandwidth control',
        description: '0 disables RC control. Otherwise choose the RC channel used for bandwidth switching.',
        control: 'number',
        valueType: 'number',
        min: 0,
        max: 16,
        group: 'RC integration'
      },
      wb_enable_listen_only_mode: {
        label: 'Listen-only mode',
        description: 'Enable passive reception without transmitting (developer/diagnostic use).',
        control: 'toggle',
        valueType: 'boolean',
        group: 'Advanced'
      },
      wb_dev_air_set_high_retransmit_count: {
        label: 'High retransmit count (developer)',
        description: 'Developer option to raise WiFi retransmit thresholds.',
        control: 'toggle',
        valueType: 'boolean',
        group: 'Advanced'
      }
    }
  }
};

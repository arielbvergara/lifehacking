# AWS S3 Configuration Setup Guide

This guide walks you through setting up AWS S3 and CloudFront for the category image upload feature.

## Prerequisites

- AWS Account with appropriate permissions
- AWS CLI installed (optional but recommended)
- Access to create S3 buckets, CloudFront distributions, and IAM policies

## Step 1: Create S3 Buckets

You'll need separate buckets for development and production environments.

### Using AWS Console

1. Navigate to S3 in AWS Console
2. Click "Create bucket"
3. Configure the bucket:
   - **Bucket name**: `lifehacking-dev` (for development)
   - **Region**: `eu-central-1` (Europe - Frankfurt)
   - **Block Public Access**: Keep all settings enabled (bucket should NOT be public)
   - **Bucket Versioning**: Optional (recommended for production)
   - **Default encryption**: Enable with SSE-S3 (AES-256)
4. Click "Create bucket"
5. Repeat for production: `lifehacking-prod`

### Using AWS CLI

```bash
# Development bucket
aws s3api create-bucket \
  --bucket lifehacking-dev \
  --region eu-central-1 \
  --create-bucket-configuration LocationConstraint=eu-central-1

# Production bucket
aws s3api create-bucket \
  --bucket lifehacking-prod \
  --region eu-central-1 \
  --create-bucket-configuration LocationConstraint=eu-central-1

# Enable encryption on development bucket
aws s3api put-bucket-encryption \
  --bucket lifehacking-dev \
  --server-side-encryption-configuration '{
    "Rules": [{
      "ApplyServerSideEncryptionByDefault": {
        "SSEAlgorithm": "AES256"
      }
    }]
  }'

# Enable encryption on production bucket
aws s3api put-bucket-encryption \
  --bucket lifehacking-prod \
  --server-side-encryption-configuration '{
    "Rules": [{
      "ApplyServerSideEncryptionByDefault": {
        "SSEAlgorithm": "AES256"
      }
    }]
  }'
```

**Note**: For regions outside `us-east-1`, you must specify `--create-bucket-configuration LocationConstraint=REGION`.

## Step 2: Configure S3 Bucket Policy

The bucket needs a policy to allow CloudFront to read objects. We'll configure this after creating the CloudFront distribution in Step 3, as the policy requires the CloudFront distribution ARN.

## Step 3: Create CloudFront Distribution with Origin Access Control (OAC)

CloudFront provides a CDN for fast image delivery worldwide. We'll use the modern Origin Access Control (OAC) instead of the legacy Origin Access Identity (OAI).

### Using AWS Console

1. Navigate to CloudFront in AWS Console
2. Click "Create distribution"

3. **Configure Origin Settings:**
   - **Origin domain**: Select your S3 bucket from dropdown (e.g., `lifehacking-dev.s3.eu-central-1.amazonaws.com`)
   - **Origin access**: Select "Origin access control settings (recommended)"
   - **Origin access control**: Click "Create new OAC"
     - **Name**: `lifehacking-s3-oac-dev`
     - **Description**: Origin access control for lifehacking category images
     - **Signing behavior**: Sign requests (recommended)
     - Click "Create"
   - **Note**: AWS will show a message that you need to update the S3 bucket policy. We'll do this in Step 4.

4. **Configure Default Cache Behavior:**
   - **Viewer protocol policy**: Redirect HTTP to HTTPS
   - **Allowed HTTP methods**: GET, HEAD, OPTIONS
   - **Cache policy**: CachingOptimized (recommended)
   - **Origin request policy**: None (or CORS-S3Origin if you need CORS)
   - **Response headers policy**: Optional (SimpleCORS if needed)
   - **Compress objects automatically**: Yes (recommended for better performance)

5. **Configure Distribution Settings:**
   - **Price class**: Choose based on your needs
     - Use all edge locations (best performance, higher cost)
     - Use only North America and Europe (good balance)
     - Use only North America, Europe, Asia, Middle East, and Africa (exclude South America and Australia)
   - **AWS WAF web ACL**: None (or select if you have WAF configured)
   - **Alternate domain name (CNAME)**: Optional (e.g., `images.yourdomain.com`)
     - If using custom domain, you must also configure DNS (Route 53 or your DNS provider)
   - **SSL certificate**: 
     - Default CloudFront certificate (*.cloudfront.net) - Free
     - Custom SSL certificate (requires AWS Certificate Manager) - For custom domains
   - **Supported HTTP versions**: HTTP/2, HTTP/3 (recommended)
   - **Default root object**: Leave empty for image storage
   - **Standard logging**: Optional but recommended for troubleshooting
     - If enabled, specify an S3 bucket for logs (must be different from origin bucket)
   - **IPv6**: Enabled (recommended)

6. Click "Create distribution"

7. **Important**: Copy the distribution domain name (e.g., `d1234567890abc.cloudfront.net`) - you'll need this for your application configuration

8. **Copy the S3 bucket policy**: After creation, CloudFront will show a banner with a "Copy policy" button. Click it to copy the bucket policy, or proceed to Step 4 to create it manually.

9. **Wait for deployment**: The distribution status will show "Deploying" initially. Wait 5-15 minutes for it to change to "Enabled" before testing.

### Using AWS CLI

```bash
# First, create an Origin Access Control
aws cloudfront create-origin-access-control \
  --origin-access-control-config '{
    "Name": "lifehacking-s3-oac-dev",
    "Description": "OAC for lifehacking category images",
    "SigningProtocol": "sigv4",
    "SigningBehavior": "always",
    "OriginAccessControlOriginType": "s3"
  }'

# Note the OAC ID from the output (e.g., E1234567890ABC)
# Save it to a variable for use in the next command
OAC_ID="E1234567890ABC"  # Replace with your actual OAC ID

# Create the CloudFront distribution configuration
cat > cloudfront-config.json <<EOF
{
  "CallerReference": "lifehacking-dev-$(date +%s)",
  "Comment": "Lifehacking Category Images CDN",
  "Enabled": true,
  "Origins": {
    "Quantity": 1,
    "Items": [
      {
        "Id": "S3-lifehacking-dev",
        "DomainName": "lifehacking-dev.s3.eu-central-1.amazonaws.com",
        "OriginAccessControlId": "${OAC_ID}",
        "S3OriginConfig": {
          "OriginAccessIdentity": ""
        },
        "ConnectionAttempts": 3,
        "ConnectionTimeout": 10
      }
    ]
  },
  "DefaultCacheBehavior": {
    "TargetOriginId": "S3-lifehacking-dev",
    "ViewerProtocolPolicy": "redirect-to-https",
    "AllowedMethods": {
      "Quantity": 3,
      "Items": ["GET", "HEAD", "OPTIONS"],
      "CachedMethods": {
        "Quantity": 2,
        "Items": ["GET", "HEAD"]
      }
    },
    "Compress": true,
    "CachePolicyId": "658327ea-f89d-4fab-a63d-7e88639e58f6",
    "TrustedSigners": {
      "Enabled": false,
      "Quantity": 0
    },
    "TrustedKeyGroups": {
      "Enabled": false,
      "Quantity": 0
    },
    "ViewerProtocolPolicy": "redirect-to-https",
    "MinTTL": 0,
    "DefaultTTL": 86400,
    "MaxTTL": 31536000
  },
  "PriceClass": "PriceClass_100",
  "HttpVersion": "http2and3",
  "IsIPV6Enabled": true
}
EOF

# Create the distribution
aws cloudfront create-distribution \
  --distribution-config file://cloudfront-config.json \
  --output json > distribution-output.json

# Extract the distribution domain name and ID
DISTRIBUTION_DOMAIN=$(cat distribution-output.json | jq -r '.Distribution.DomainName')
DISTRIBUTION_ID=$(cat distribution-output.json | jq -r '.Distribution.Id')

echo "Distribution created successfully!"
echo "Distribution ID: $DISTRIBUTION_ID"
echo "Distribution Domain: $DISTRIBUTION_DOMAIN"
echo ""
echo "Add this to your appsettings.json:"
echo "  \"CloudFront\": {"
echo "    \"Domain\": \"$DISTRIBUTION_DOMAIN\""
echo "  }"
echo ""
echo "Wait 5-15 minutes for deployment to complete, then proceed to Step 4."

# Check distribution status
echo ""
echo "To check deployment status, run:"
echo "aws cloudfront get-distribution --id $DISTRIBUTION_ID --query 'Distribution.Status'"
```

**Important Notes for CLI Setup:**

1. The `CachePolicyId` `658327ea-f89d-4fab-a63d-7e88639e58f6` is AWS's managed "CachingOptimized" policy
2. `PriceClass_100` uses only North America and Europe edge locations (cost-effective)
   - Use `PriceClass_All` for all edge locations (best performance)
   - Use `PriceClass_200` for North America, Europe, Asia, Middle East, and Africa
3. The distribution will take 5-15 minutes to deploy
4. Save the distribution ID and domain name for the next steps

## Step 4: Update S3 Bucket Policy for CloudFront OAC

Now that the CloudFront distribution is created, update the S3 bucket policy to allow CloudFront to access objects.

### Optional: Configure Custom Domain (Before Bucket Policy)

If you want to use a custom domain like `images.yourdomain.com` instead of the CloudFront domain:

1. **Request SSL Certificate in AWS Certificate Manager (ACM)**:
   - Go to ACM in **us-east-1 region** (required for CloudFront)
   - Request a public certificate for your domain (e.g., `images.yourdomain.com`)
   - Validate domain ownership via DNS or email
   - Wait for certificate status to be "Issued"

2. **Add Custom Domain to CloudFront**:
   - Edit your CloudFront distribution
   - Under "Alternate domain names (CNAMEs)", add `images.yourdomain.com`
   - Under "Custom SSL certificate", select your ACM certificate
   - Save changes

3. **Update DNS**:
   - In your DNS provider (Route 53, Cloudflare, etc.)
   - Create a CNAME record: `images.yourdomain.com` → `d1234567890abc.cloudfront.net`
   - Or use Route 53 Alias record pointing to the CloudFront distribution

4. **Update Application Configuration**:
   - Use your custom domain in `appsettings.json`:
     ```json
     "CloudFront": {
       "Domain": "images.yourdomain.com"
     }
     ```

Now that the CloudFront distribution is created, update the S3 bucket policy to allow CloudFront to access objects.

### Get Your CloudFront Distribution ARN

You'll need your CloudFront distribution ARN. You can find it in:
- AWS Console: CloudFront → Your distribution → General tab
- Format: `arn:aws:cloudfront::ACCOUNT_ID:distribution/DISTRIBUTION_ID`

Or get it via CLI:
```bash
aws cloudfront list-distributions --query "DistributionList.Items[?Comment=='Lifehacking Category Images CDN'].ARN" --output text
```

### Apply Bucket Policy

Replace the placeholders:
- `YOUR_ACCOUNT_ID` - Your AWS account ID (12 digits)
- `YOUR_DISTRIBUTION_ID` - Your CloudFront distribution ID (e.g., E1234567890ABC)

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "AllowCloudFrontServicePrincipal",
      "Effect": "Allow",
      "Principal": {
        "Service": "cloudfront.amazonaws.com"
      },
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::lifehacking-dev/*",
      "Condition": {
        "StringEquals": {
          "AWS:SourceArn": "arn:aws:cloudfront::YOUR_ACCOUNT_ID:distribution/YOUR_DISTRIBUTION_ID"
        }
      }
    }
  ]
}
```

### Apply via AWS Console

1. Go to S3 → Your bucket → Permissions tab
2. Scroll to "Bucket policy"
3. Click "Edit"
4. Paste the policy above (with your actual values)
5. Click "Save changes"

### Apply via AWS CLI

```bash
# Replace with your actual values
ACCOUNT_ID="123456789012"
DISTRIBUTION_ID="E1234567890ABC"
BUCKET_NAME="lifehacking-dev"

# Create the policy
cat > bucket-policy.json <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "AllowCloudFrontServicePrincipal",
      "Effect": "Allow",
      "Principal": {
        "Service": "cloudfront.amazonaws.com"
      },
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::${BUCKET_NAME}/*",
      "Condition": {
        "StringEquals": {
          "AWS:SourceArn": "arn:aws:cloudfront::${ACCOUNT_ID}:distribution/${DISTRIBUTION_ID}"
        }
      }
    }
  ]
}
EOF

# Apply the policy
aws s3api put-bucket-policy --bucket $BUCKET_NAME --policy file://bucket-policy.json
```

### Verify the Configuration

After applying the bucket policy:

1. Wait 5-10 minutes for CloudFront to fully deploy
2. Check CloudFront distribution status (should show "Deployed")
3. Test by uploading an image via your application
4. Verify the CloudFront URL returns the image (not 403 Forbidden)

## Step 5: Create IAM User/Role for Application

The application needs permissions to upload images to S3.

### Create IAM Policy

Create a policy named `LifehackingS3UploadPolicy`:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "AllowS3Upload",
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:PutObjectAcl"
      ],
      "Resource": [
        "arn:aws:s3:::lifehacking-dev/*",
        "arn:aws:s3:::lifehacking-prod/*"
      ]
    },
    {
      "Sid": "AllowListBucket",
      "Effect": "Allow",
      "Action": "s3:ListBucket",
      "Resource": [
        "arn:aws:s3:::lifehacking-dev",
        "arn:aws:s3:::lifehacking-prod"
      ]
    }
  ]
}
```

### Option A: IAM User (for local development)

1. Create IAM user: `lifehacking-app-dev`
2. Attach the policy created above
3. Create access keys
4. Save the Access Key ID and Secret Access Key

### Option B: IAM Role (for EC2/ECS/Lambda)

1. Create IAM role: `LifehackingAppRole`
2. Attach the policy created above
3. Attach this role to your EC2 instance, ECS task, or Lambda function

## Step 6: Configure Application Settings

### Development Environment

Update `lifehacking/WebAPI/appsettings.Development.json`:

```json
{
  "AWS": {
    "S3": {
      "BucketName": "lifehacking-dev",
      "Region": "eu-central-1"
    },
    "CloudFront": {
      "Domain": "d1234567890abc.cloudfront.net"
    }
  }
}
```

### Production Environment

Update `lifehacking/WebAPI/appsettings.json`:

```json
{
  "AWS": {
    "S3": {
      "BucketName": "lifehacking-prod",
      "Region": "eu-central-1"
    },
    "CloudFront": {
      "Domain": "d9876543210xyz.cloudfront.net"
    }
  }
}
```

## Step 7: Set Up AWS Credentials

### Local Development

**Option 1: Environment Variables**

Add to your shell profile (`~/.zshrc`, `~/.bashrc`, etc.):

```bash
export AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE
export AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
export AWS_REGION=eu-central-1
```

Then reload: `source ~/.zshrc`

**Option 2: AWS Credentials File**

Create/edit `~/.aws/credentials`:

```ini
[default]
aws_access_key_id = AKIAIOSFODNN7EXAMPLE
aws_secret_access_key = wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY

[lifehacking-dev]
aws_access_key_id = AKIAIOSFODNN7EXAMPLE
aws_secret_access_key = wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
```

Create/edit `~/.aws/config`:

```ini
[default]
region = eu-central-1

[profile lifehacking-dev]
region = eu-central-1
```

To use a specific profile, set:
```bash
export AWS_PROFILE=lifehacking-dev
```

### Production Deployment

**For EC2/ECS:**
1. Attach the IAM role to your instance/task
2. No credentials needed in environment variables or files
3. The AWS SDK will automatically use the instance role

**For Docker/Kubernetes:**
- Use AWS Secrets Manager or Parameter Store
- Mount secrets as environment variables
- Or use IAM Roles for Service Accounts (IRSA) in EKS

## Step 8: Test the Configuration

### Verify AWS Credentials

```bash
# Test AWS CLI access
aws sts get-caller-identity

# Test S3 access
aws s3 ls s3://lifehacking-dev/

# Upload a test file
echo "test" > test.txt
aws s3 cp test.txt s3://lifehacking-dev/test.txt
```

### Test the Application

1. Start the application:
   ```bash
   dotnet run --project lifehacking/WebAPI/WebAPI.csproj
   ```

2. Test the upload endpoint:
   ```bash
   curl -X POST https://localhost:5001/api/admin/categories/images \
     -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
     -F "file=@/path/to/test-image.jpg"
   ```

3. Verify the response contains:
   - `imageUrl` (CloudFront URL)
   - `imageStoragePath` (S3 key)
   - `originalFileName`
   - `contentType`
   - `fileSizeBytes`
   - `uploadedAt`

4. Verify the image is accessible via the CloudFront URL

## Step 9: Configure S3 Lifecycle Policies (Optional)

To manage storage costs, configure lifecycle policies to transition or delete old images.

### Example: Transition to Glacier after 90 days

```json
{
  "Rules": [
    {
      "Id": "TransitionOldImages",
      "Status": "Enabled",
      "Prefix": "categories/",
      "Transitions": [
        {
          "Days": 90,
          "StorageClass": "GLACIER"
        }
      ]
    }
  ]
}
```

Apply via AWS Console:
1. Go to S3 bucket → "Management" → "Lifecycle rules"
2. Create lifecycle rule with the configuration above

## Step 10: Enable Monitoring and Logging (Recommended)

Proper monitoring helps troubleshoot issues and optimize performance.

### Enable S3 Server Access Logging

Track all requests to your S3 bucket:

1. Create a separate S3 bucket for logs (e.g., `lifehacking-logs`)
2. Go to your image bucket → "Properties" → "Server access logging"
3. Click "Edit" and enable logging
4. Select your logs bucket as the target
5. Set prefix: `s3-access-logs/`

### Enable CloudFront Standard Logging

Track all CloudFront requests:

1. Create or use your logs bucket
2. Go to CloudFront → Your distribution → "General" tab
3. Click "Edit"
4. Under "Standard logging", select "On"
5. Choose your S3 bucket for logs
6. Set log prefix: `cloudfront-logs/`
7. Save changes

### Set Up CloudWatch Alarms

Monitor for issues and unusual activity:

```bash
# Create alarm for high 4xx error rate
aws cloudwatch put-metric-alarm \
  --alarm-name lifehacking-cloudfront-high-4xx \
  --alarm-description "Alert when CloudFront 4xx error rate is high" \
  --metric-name 4xxErrorRate \
  --namespace AWS/CloudFront \
  --statistic Average \
  --period 300 \
  --evaluation-periods 2 \
  --threshold 5.0 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=DistributionId,Value=YOUR_DISTRIBUTION_ID

# Create alarm for high 5xx error rate
aws cloudwatch put-metric-alarm \
  --alarm-name lifehacking-cloudfront-high-5xx \
  --alarm-description "Alert when CloudFront 5xx error rate is high" \
  --metric-name 5xxErrorRate \
  --namespace AWS/CloudFront \
  --statistic Average \
  --period 300 \
  --evaluation-periods 2 \
  --threshold 1.0 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=DistributionId,Value=YOUR_DISTRIBUTION_ID

# Create alarm for unusual request volume
aws cloudwatch put-metric-alarm \
  --alarm-name lifehacking-s3-high-requests \
  --alarm-description "Alert when S3 request volume is unusually high" \
  --metric-name AllRequests \
  --namespace AWS/S3 \
  --statistic Sum \
  --period 3600 \
  --evaluation-periods 1 \
  --threshold 10000 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=BucketName,Value=lifehacking-prod
```

### Enable AWS CloudTrail (Security Audit)

Track all API calls for security and compliance:

1. Go to CloudTrail in AWS Console
2. Create a trail or use an existing one
3. Ensure S3 and CloudFront events are logged
4. Store logs in a secure S3 bucket with encryption
5. Enable log file validation for integrity

### View CloudFront Metrics

Monitor performance in CloudFront console:

1. Go to CloudFront → Your distribution → "Monitoring" tab
2. View metrics:
   - Requests
   - Bytes downloaded
   - Error rates (4xx, 5xx)
   - Cache hit rate
3. Set up CloudWatch dashboards for visualization

## Security Best Practices

1. **Never commit AWS credentials to version control**
   - Add `.aws/` to `.gitignore`
   - Use environment variables or IAM roles

2. **Use least privilege IAM policies**
   - Only grant `s3:PutObject` permission, not `s3:*`
   - Restrict to specific bucket ARNs

3. **Enable S3 bucket encryption**
   - Use SSE-S3 (AES-256) or SSE-KMS for encryption at rest

4. **Keep S3 buckets private**
   - Block all public access
   - Use CloudFront with OAI for public access

5. **Enable CloudFront HTTPS only**
   - Redirect HTTP to HTTPS
   - Use TLS 1.2 or higher

6. **Monitor and audit**
   - Enable S3 access logging
   - Enable CloudTrail for API audit logs
   - Set up CloudWatch alarms for unusual activity

7. **Rotate credentials regularly**
   - Rotate IAM access keys every 90 days
   - Use temporary credentials when possible

## Troubleshooting

### Error: "Access Denied" when uploading

**Cause**: IAM user/role lacks S3 PutObject permission

**Solution**: 
- Verify IAM policy includes `s3:PutObject` action
- Check bucket policy doesn't deny uploads
- Verify credentials are correctly configured

### Error: "Bucket does not exist"

**Cause**: Bucket name in configuration doesn't match actual bucket

**Solution**:
- Verify bucket name in `appsettings.json`
- Check bucket exists in correct AWS region
- Ensure region in configuration matches bucket region

### Error: "The AWS Access Key Id you provided does not exist"

**Cause**: Invalid or expired AWS credentials

**Solution**:
- Verify `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`
- Check credentials file (`~/.aws/credentials`)
- Rotate and update access keys if expired

### CloudFront URL returns 403 Forbidden

**Cause**: CloudFront doesn't have permission to read from S3, or the bucket policy is incorrect

**Solution**:
- Verify bucket policy allows CloudFront service principal
- Check the `AWS:SourceArn` condition matches your distribution ARN
- Verify Origin Access Control (OAC) is properly configured in CloudFront
- Check CloudFront distribution origin settings point to correct S3 bucket
- Wait for CloudFront distribution to fully deploy (can take 15-20 minutes)
- Verify the S3 bucket policy was applied successfully

### Images upload but CloudFront URL doesn't work

**Cause**: CloudFront distribution not fully deployed or misconfigured

**Solution**:
- Check CloudFront distribution status (should be "Deployed")
- Verify origin domain points to correct S3 bucket
- Test direct S3 URL first (if bucket policy allows)
- Check CloudFront cache behavior settings

## Cost Estimation

Approximate monthly costs for moderate usage:

- **S3 Storage**: $0.023 per GB (first 50 TB)
  - 10,000 images × 500 KB average = 5 GB = $0.12/month

- **S3 PUT Requests**: $0.005 per 1,000 requests
  - 10,000 uploads = $0.05/month

- **CloudFront Data Transfer**: $0.085 per GB (first 10 TB)
  - 100,000 image views × 500 KB = 50 GB = $4.25/month

- **CloudFront Requests**: $0.0075 per 10,000 HTTPS requests
  - 100,000 requests = $0.75/month

**Total estimated cost**: ~$5-10/month for moderate usage

## Additional Resources

- [AWS S3 Documentation](https://docs.aws.amazon.com/s3/)
- [AWS CloudFront Documentation](https://docs.aws.amazon.com/cloudfront/)
- [AWS SDK for .NET Documentation](https://docs.aws.amazon.com/sdk-for-net/)
- [IAM Best Practices](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html)
- [S3 Security Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html)
